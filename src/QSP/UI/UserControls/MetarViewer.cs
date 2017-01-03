﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QSP.LibraryExtension;
using QSP.Metar;
using QSP.RouteFinding.Airports;
using QSP.UI.Utilities;
using QSP.WindAloft;

namespace QSP.UI.UserControls
{
    public partial class MetarViewer : UserControl
    {
        private Image processingImage;

        private Func<string> origGetter;
        private Func<string> destGetter;
        private Func<IEnumerable<string>> altnGetter;

        public MetarViewer()
        {
            InitializeComponent();
        }

        public void Init(
           Func<string> origGetter,
           Func<string> destGetter,
           Func<IEnumerable<string>> altnGetter)
        {
            this.origGetter = origGetter;
            this.destGetter = destGetter;
            this.altnGetter = altnGetter;

            SetImages();
            metarLastUpdatedLbl.Text = "";
            downloadAllBtn.Click += (s, e) => UpdateAllMetarTaf();
            IcaoTxtBox.TextChanged += (s, e) => DownloadMetarTaf();
        }

        private void SetImages()
        {
            processingImage = ImageUtil.Resize(Properties.Resources.processing, statusPicBox.Size);
        }

        private string Icao => IcaoTxtBox.Text.Trim().ToUpper();

        private async Task DownloadMetarTaf()
        {
            var icaoCode = Icao;
            statusPicBox.Image = processingImage;
            metarTafRichTxtBox.Text = "";
            var result = await Task.Factory.StartNew(() => MetarDownloader.GetMetarTaf(icaoCode));

            if (result == null)
            {
                statusPicBox.SetImageHighQuality(Properties.Resources.deleteIconLarge);
                return;
            }

            if (Icao == icaoCode)
            {
                metarTafRichTxtBox.Text = result;
                SetUpdateTime();
                statusPicBox.SetImageHighQuality(Properties.Resources.checkIconLarge);
            }
        }

        private void SetUpdateTime()
        {
            metarLastUpdatedLbl.Text = $"Last Updated : {DateTime.Now}";
        }

        private async Task UpdateAllMetarTaf()
        {
            downloadAllBtn.Enabled = false;

            var allTasks = new[] { OrigTask(), DestTask() }.Concat(AltnTask());
            var result = await Task.WhenAll(allTasks);

            metarTafRichTxtBox.Text = string.Join("\n\n", result);
            SetUpdateTime();
            downloadAllBtn.Enabled = true;
        }

        private Task<string> OrigTask()
        {
            return Task.Factory.StartNew(
                () => MetarDownloader.TryGetMetarTaf(origGetter()));
        }

        private Task<string> DestTask()
        {
            return Task.Factory.StartNew(
                () => MetarDownloader.TryGetMetarTaf(destGetter()));
        }

        private IEnumerable<Task<string>> AltnTask()
        {
            return altnGetter().Select(
                i => Task.Factory.StartNew(
                    () => MetarDownloader.TryGetMetarTaf(i)));
        }
    }
}
