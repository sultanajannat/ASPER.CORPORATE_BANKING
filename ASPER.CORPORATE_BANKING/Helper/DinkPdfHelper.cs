using DinkToPdf;
using DinkToPdf.Contracts;

namespace ASPER.CORPORATE_BANKING.Helper
{
    public class DinkPdfHelper
    {
        private static readonly IConverter _converter = new SynchronizedConverter(new PdfTools());

        public byte[] A4_PortraitPDFCustomDate(string url, string username, DateTime? date)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var printDate = date?.ToString("yyyy-MM-dd") + " " + time;

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 20, Right = 20 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                WebSettings = { DefaultEncoding = "utf-8" },
                FooterSettings = { 
                    FontSize = 10, 
                    FontName = "Arial",
                    Right = "Page [page] of [toPage]", 
                    Left = $"Print At: {printDate}",
                    Center = $"Print By: {username}",
                    Line = true,
                    Spacing = 2.81
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] A4_PortraitPDFForOther(string url, string username, DateTime? date)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var printDate = date?.ToString("yyyy-MM-dd") + " " + time;

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 4, Left = 5, Right = 5 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {printDate}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] A4_PortraitPDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 4, Left = 20, Right = 20 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] A4_LandscapePDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Landscape,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 4, Left = 12, Right = 12 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] Legal_PortraitPDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.Legal,
                Margins = new MarginSettings { Top = 3, Bottom = 8, Left = 6, Right = 6 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] Legal_LandscapePDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Landscape,
                PaperSize = PaperKind.Legal,
                Margins = new MarginSettings { Top = 5, Bottom = 3, Left = 2, Right = 2 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] GeneratePDFVoucher(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 14, Bottom = 9, Left = 14, Right = 14 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] A3_LandscapePDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Landscape,
                PaperSize = PaperKind.A3,
                Margins = new MarginSettings { Top = 5, Bottom = 5, Left = 2, Right = 2 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public byte[] A2_LandscapePDF(string url, string username)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Landscape,
                PaperSize = PaperKind.A2,
                Margins = new MarginSettings { Top = 5, Bottom = 5, Left = 2, Right = 2 },
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                Page = url,
                FooterSettings = { 
                    FontSize = 10, 
                    Left = $"Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}", 
                    Center = $"Print By: {username}", 
                    Right = "Page [page] of [toPage]" 
                }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }
    }
}

