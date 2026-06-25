using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace VibroMonitor.Views
{
    public partial class HighChartWindow : Window
    {
        private readonly string _htmlContent;
        private readonly double? _hi;
        private readonly double? _hiHi;
        private string? _tempHtmlFilePath;

        public HighChartWindow(string title, List<(long time, double value)> data, double? hi = null, double? hiHi = null)
        {
            InitializeComponent();

            _hi = hi;
            _hiHi = hiHi;

            _htmlContent = BuildHtml(title, data);

            // Defer navigation until WebView2 is initialized
            Loaded += HighChartWindow_Loaded;
        }

        private async void HighChartWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                var webview = FindName("Browser") as Microsoft.Web.WebView2.Wpf.WebView2;
                if (webview == null)
                {
                    MessageBox.Show("WebView2 control not found.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (webview.CoreWebView2 == null)
                {
                    await webview.EnsureCoreWebView2Async();
                }

                // enable dev tools and open for debug builds to inspect console
                try
                {
                    webview.CoreWebView2.Settings.AreDevToolsEnabled = true;
#if DEBUG
                    webview.CoreWebView2.OpenDevToolsWindow();
#endif
                }
                catch { }
                // rely on in-page #err element and polling to detect script load errors

                // NavigateToString can throw ArgumentException for large HTML content when marshaling to COM.
                // Try it first, fall back to writing a temp .html file and navigating to its file:// URI.
                try
                {
                    webview.NavigateToString(_htmlContent);
                }
                catch (ArgumentException)
                {
                    try
                    {
                        // create temp file and navigate to it
                        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".html");
                        File.WriteAllText(temp, _htmlContent, Encoding.UTF8);
                        _tempHtmlFilePath = temp;
                        // use CoreWebView2.Navigate to ensure navigation of file URI
                        var uri = new Uri(temp).AbsoluteUri;
                        if (webview.CoreWebView2 != null)
                        {
                            webview.CoreWebView2.Navigate(uri);
                        }
                        else
                        {
                            webview.Source = new Uri(uri);
                        }
                    }
                    catch (Exception writeEx)
                    {
                        // fall back to showing the original error to the user
                        MessageBox.Show($"Не удалось загрузить HTML во внешнюю страницу: {writeEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // poll for Highcharts being defined (give script time to load)
                bool loaded = false;
                string lastErr = string.Empty;
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(300);
                    try
                    {
                        var res = await webview.ExecuteScriptAsync("(function(){ if(window.Highcharts) return 'ok'; var e=document.getElementById('err'); return e?e.textContent:''; })();");
                        if (!string.IsNullOrWhiteSpace(res) && res != "null" && res != "\"\"")
                        {
                            var txt = res.Trim();
                            if (txt.StartsWith("\"") && txt.EndsWith("\"")) txt = txt.Substring(1, txt.Length - 2);
                            if (txt == "ok") { loaded = true; break; }
                            lastErr = txt;
                        }
                    }
                    catch { }
                }

                if (!loaded)
                {
                    var msg = string.IsNullOrEmpty(lastErr) ? "Highcharts не загрузился. Проверьте доступ к CDN или WebView2 runtime." : lastErr;
                    MessageBox.Show($"Highcharts не определён: {msg}", "HighCharts error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать WebView2: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildHtml(string title, List<(long time, double value)> data)
        {
            var dataArray = string.Join(",", data.Select(d => $"[{d.time},{d.value.ToString(System.Globalization.CultureInfo.InvariantCulture)}]"));

            // compute data min/max and include thresholds so plotLines are visible
            double? dataMax = null;
            double? dataMin = null;
            if (data != null && data.Count > 0)
            {
                dataMax = data.Max(d => d.value);
                dataMin = data.Min(d => d.value);
            }
            double? desiredMax = dataMax;
            double? desiredMin = dataMin;
            if (_hiHi.HasValue) desiredMax = Math.Max(desiredMax ?? double.MinValue, _hiHi.Value);
            if (_hi.HasValue) desiredMax = Math.Max(desiredMax ?? double.MinValue, _hi.Value);
            if (_hi.HasValue) desiredMin = Math.Min(desiredMin ?? double.MaxValue, _hi.Value);
            if (_hiHi.HasValue) desiredMin = Math.Min(desiredMin ?? double.MaxValue, _hiHi.Value);
            // add small margin
            if (desiredMax.HasValue && desiredMin.HasValue)
            {
                var range = desiredMax.Value - desiredMin.Value;
                if (range <= 0)
                {
                    // flat data, add absolute margin
                    desiredMax = desiredMax + 1.0;
                    desiredMin = desiredMin - 1.0;
                }
                else
                {
                    desiredMax = desiredMax + range * 0.1;
                    desiredMin = desiredMin - range * 0.1;
                }
            }
            string desiredMaxJs = desiredMax.HasValue ? desiredMax.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null";
            string desiredMinJs = desiredMin.HasValue ? desiredMin.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null";

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\" />");
            sb.AppendLine($"    <title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
            // basic styles to ensure the container fills the view
            //sb.AppendLine("<script src=\"https://code.highcharts.com/highcharts.js\"> </script>");
            sb.AppendLine("    <style>html,body{height:100%;width:100%;margin:0;padding:0}body{font-family:Segoe UI,Arial,sans-serif;background:#ffffff}</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div id=\"container\" style=\"height:100%; width:99%;\"></div>");
            sb.AppendLine("<div id=\"err\" style=\"position:fixed;left:8px;bottom:8px;color:#900;\"></div>");
            sb.AppendLine("<script>");
            sb.AppendLine("(function(){try{");
            sb.AppendLine("    var data = [");
            sb.AppendLine(dataArray + "];");
            sb.AppendLine("    if(!data || data.length===0){document.getElementById('container').innerHTML='<div style=\\\"display:flex;height:100%;align-items:center;justify-content:center;color:#666;\\\">Нет данных за выбранный период</div>'; return;} ");
            // prepare plotLines for thresholds
            var plotLines = new StringBuilder();
            if (_hi.HasValue)
            {
                plotLines.Append($"{{ value: {_hi.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, color: 'yellow', width: 2, zIndex: 5, dashStyle: 'ShortDash', label: {{ text: 'Hi', align: 'right', style: {{ color: 'black', fontWeight: 'bold' }} }} }},");
            }
            if (_hiHi.HasValue)
            {
                plotLines.Append($"{{ value: {_hiHi.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}, color: 'red', width: 2, zIndex: 6, dashStyle: 'ShortDash', label: {{ text: 'HiHi', align: 'right', style: {{ color: 'black', fontWeight: 'bold' }} }} }},");
            }

            // use serialized title as JS string
            var titleJson = System.Text.Json.JsonSerializer.Serialize(title);
            // Tooltip: show 'sensor name: value'
            sb.AppendLine("    function initChart(){ try{ Highcharts.stockChart('container',  { chart:{height:'55%'},rangeSelector:{selected:1}, title:{ text: " + titleJson + " }, yAxis: { opposite: false, min: " + desiredMinJs + ", max: " + desiredMaxJs + ", plotLines: [" + plotLines.ToString() + "] }, tooltip: { formatter: function() { return this.series.name + ': ' + Highcharts.numberFormat(this.y, 2); } }, series:[{ name:" + titleJson + ", data:data }] }); }catch(e){console.error(e); document.getElementById('err').textContent='JS error: '+e.message;} }");
            // If a local highstock.js is present in the application folder, it will be injected inline.
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "highstock.js");
            if (File.Exists(localPath))
            {
                var localScript = File.ReadAllText(localPath);
                sb.AppendLine("    // local highstock injected\n");
                sb.AppendLine("    (function(){\n");
                sb.AppendLine(localScript);
                sb.AppendLine("\n    initChart();})();");
            }
            else
            {
                sb.AppendLine("    var s = document.createElement('script'); s.src='https://code.highcharts.com/stock/highstock.js'; s.onload = initChart; s.onerror = function(ev){ document.getElementById('err').textContent = 'Failed to load Highcharts script.'; }; document.head.appendChild(s);");
            }
            sb.AppendLine("}catch(e){console.error(e);document.getElementById('err').textContent='JS error: '+e.message;}})();");
            sb.AppendLine("</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
