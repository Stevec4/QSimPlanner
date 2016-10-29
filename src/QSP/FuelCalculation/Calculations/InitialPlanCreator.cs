﻿using System;
using System.Collections.Generic;
using QSP.Common;
using QSP.FuelCalculation.FuelDataNew;
using QSP.FuelCalculation.Results;
using QSP.FuelCalculation.Results.Nodes;
using QSP.LibraryExtension;
using QSP.MathTools;
using QSP.MathTools.Vectors;
using QSP.RouteFinding.Airports;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.Data.Interfaces;
using QSP.RouteFinding.Routes;
using QSP.WindAloft;
using static QSP.AviationTools.Constants;
using static QSP.AviationTools.SpeedConversion;
using static QSP.MathTools.Doubles;
using static QSP.WindAloft.GroundSpeedCalculation;
using static System.Math;
using static QSP.FuelCalculation.Calculations.NextPlanNodeParameter;

namespace QSP.FuelCalculation.Calculations
{
    // The units of variables used in this class is specified in 
    // VariableUnitStandard.txt.

    /// <summary>
    /// Creates the list of PlanNodes for the route. The route starts at origin
    /// at its optimal cruising altitude (instead of the airport elevation).
    /// Step climbs and descent to destination are included. 
    /// </summary>
    public class InitialPlanCreator
    {
        private readonly double deltaT = 0.5;    // Time in minute
        private readonly AirportManager airportList;
        private readonly CrzAltProvider altProvider;
        private readonly IWindTableCollection windTable;
        private readonly Route route;
        private readonly FuelDataNew.FuelDataItem fuelData;
        private readonly double zfw;
        private readonly double landingFuel;
        private readonly double maxAlt;

        public InitialPlanCreator(
            AirportManager airportList,
            CrzAltProvider altProvider,
            IWindTableCollection windTable,
            Route route,
            FuelDataNew.FuelDataItem fuelData,
            double zfw,
            double landingFuel,
            double maxAlt)
        {
            if (route.Count < 2) throw new ArgumentException();

            this.airportList = airportList;
            this.altProvider = altProvider;
            this.windTable = windTable;
            this.route = route;
            this.fuelData = fuelData;
            this.zfw = zfw;
            this.landingFuel = landingFuel;
            this.maxAlt = maxAlt;
        }

        public List<PlanNode> Create()
        {
            // We compute the flight backwards - from destination to origin.

            var planNodes = new List<PlanNode>();
            
            var prevPlanNode = new PlanNode(
                route.Last,
                windTable,
                route.Last.Previous.Value.Waypoint,
                route.Last,
                route.Last.Value.Waypoint,
                DestElevationFt(),
                zfw + landingFuel,
                landingFuel,
                0.0,
                fuelData.DescendKias);

            planNodes.Add(prevPlanNode);


            // ================ Declare variables ====================
            
            LinkedListNode<RouteNode> node;
            Waypoint prevWpt;
            ICoordinate prevCoord, currentCoord;
            double grossWt, timeRemain, alt, fuelOnBoard, optCrzAlt,
                atcAllowedAlt, targetAlt, fuelFlow, descentGrad, timeToCrzAlt,
                timeToNextWpt, stepTime, descentRate, stepDis, kias, ktas, gs;
            bool isDescending;
            PlanNode prevPlanNode;
            Vector3D v;

            // ================ Initialize variables ===================
            node = route.Last;
            prevWpt = node.Previous.Value.Waypoint;
            v = v2;
            grossWt = zfw + landingFuel;
            timeRemain = 0.0;
            alt = DestElevationFt();
            fuelOnBoard = landingFuel;
            kias = fuelData.DescendKias;
            ktas = Ktas(kias, alt);
            gs = GetGS(windTable, alt, ktas, v1, v2, v);
            prevPlanNode = new PlanNode(node.Value, timeRemain,
                alt, ktas, gs, fuelOnBoard);
            prevCoord = prevPlanNode.Coordinate;
            planNodes.Add(prevPlanNode);

            while (prevWpt != null)
            {
                // Continue the loop if we have not reached origin airport.

                // Prepare the required parameters for the given step.
                grossWt = prevPlanNode.FuelOnBoard + zfw;
                optCrzAlt = fuelData.OptCruiseAltFt(grossWt);
                atcAllowedAlt = altProvider.ClosestAltitudeFtFrom(
                    prevWpt, prevCoord, optCrzAlt);
                targetAlt = Min(atcAllowedAlt, maxAlt);
                isDescending = Abs(alt - targetAlt) > 0.1;

                if (isDescending)
                {
                    fuelFlow = fuelData.DescentFuelPerMinKg(grossWt);
                    descentGrad = fuelData.DescentGradient(grossWt);
                    kias = fuelData.DescendKias;
                    ktas = Ktas(kias, alt);
                    gs = GetGS(windTable, alt, ktas, v1, v2, v);
                    descentRate = descentGrad * ktas / 60.0 * NmFtRatio;
                    timeToCrzAlt = (targetAlt - alt) / descentRate;
                }
                else
                {
                    fuelFlow = fuelData.CruiseFuelPerMinKg(grossWt);
                    descentGrad = 0.0;
                    kias = fuelData.CruiseKias(grossWt);
                    ktas = Ktas(kias, alt);
                    gs = GetGS(windTable, alt, ktas, v1, v2, v);
                    descentRate = 0.0;
                    timeToCrzAlt = double.PositiveInfinity;
                }

                timeToNextWpt = prevCoord.Distance(prevWpt) / gs * 60.0;

                double[] times = { deltaT, timeToCrzAlt, timeToNextWpt };
                int minIndex = times.MinIndex();
                stepTime = times[minIndex];
                stepDis = stepTime * ktas / 60.0;

                // Node to add to flight plan.
                object nodeVal = null;

                switch (minIndex)
                {
                    case 0:
                        currentCoord = GetV(prevCoord, prevWpt, stepDis);
                        nodeVal = new IntermediateNode(currentCoord);
                        break;

                    case 1:
                        currentCoord = GetV(prevCoord, prevWpt, stepDis);
                        nodeVal = new TodNode(currentCoord);
                        break;

                    case 2:
                        // Choose the next waypoint as current point.
                        currentCoord = prevWpt;
                        nodeVal = node.Previous.Value;

                        // Update next waypoint.
                        node = node.Previous;
                        prevWpt = node.Value.Waypoint;
                        break;

                    default:
                        throw new UnexpectedExecutionStateException();
                }

                // Updating the value for the PlanNode.
                timeRemain += stepTime;
                alt += stepTime * descentRate;
                fuelOnBoard += stepTime * fuelFlow;

                prevPlanNode = new PlanNode(nodeVal,
                    timeRemain, alt, ktas, gs, fuelOnBoard);
                prevCoord = prevPlanNode.Coordinate;
                v = prevCoord.ToVector3D();
                planNodes.Add(prevPlanNode);
            }

            planNodes.Reverse();
            return planNodes;
        }

        private NextPlanNodeParameter GetNextPara(PlanNode node)
        {
            double optCrzAlt = fuelData.OptCruiseAltFt(node.GrossWt);
            double atcAllowedAlt = altProvider.ClosestAltitudeFtFrom(
                node.PrevWaypoint, node.Coordinate, optCrzAlt);
            double targetAlt = Min(atcAllowedAlt, maxAlt);
            double altDiff = node.Alt - targetAlt;
            VerticalMode mode = GetMode(altDiff);

            
        }

        private static VerticalMode GetMode(double altDiff)
        {
            const double diffCriteria = 0.1;

            if (altDiff > diffCriteria)
            {
                return VerticalMode.Descent;
            }
            else if (altDiff < -diffCriteria)
            {
                return VerticalMode.Climb;
            }
            else
            {
                return VerticalMode.Cruise;
            }
        }

        private double DestElevationFt()
        {
            var icao = route.Last.Value.Waypoint.ID.Substring(0, 4).ToUpper();
            return airportList[icao].Elevation;
        }

        private static ICoordinate GetV(ICoordinate p1, ICoordinate p2,
            double disNm)
        {
            return EarthGeometry.GetV(
                p1.ToVector3D(),
                p2.ToVector3D(),
                disNm / EarthRadiusNm).ToLatLon();
        }
    }
}