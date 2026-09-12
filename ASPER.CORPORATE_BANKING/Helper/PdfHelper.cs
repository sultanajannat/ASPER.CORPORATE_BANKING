using NReco.PdfGenerator;

namespace ASPER.CORPORATE_BANKING.Helper
{
    public class PdfHelper
    {
        public PdfHelper()
        {

        }


        public byte[] A4_PortraitPDFCustomDate(string url, string username, DateTime? date)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var printDate = date?.ToString("yyyy-MM-dd") + " " + time;

            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Portrait;
            pdfGenerator.Size = PageSize.A4;
            // Adjust margins for a full-page feel
            pdfGenerator.Margins = new PageMargins { Top = 15, Bottom = 15, Left = 20, Right = 20 };

            // --- USE THIS NEW, ROBUST FOOTER ---
            pdfGenerator.PageFooterHtml = $@"
        <div style='width:100%; font-family: Arial, sans-serif; font-size: 10px; color: #777;'>
            <table style='width: 100%; border-top: 1px solid #EAECEF; padding-top: 8px;'>
                <tr>
                    <td style='text-align: left; width: 33.3%;'>Print At: {printDate}</td>
                    <td style='text-align: center; width: 33.3%;'>Print By: {username}</td>
                    <td style='text-align: right; width: 33.3%;'>Page <span class='page'></span> of <span class='topage'></span></td>
                </tr>
            </table>
        </div>";

            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }

        public byte[] A4_PortraitPDFForOther(string url, string username, DateTime? date)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var printDate = date?.ToString("yyyy-MM-dd") + " " + time;

            var pdfGenerator = new HtmlToPdfConverter();

            // Set some optional parameters (if needed)
            pdfGenerator.Orientation = PageOrientation.Portrait;
            pdfGenerator.Size = PageSize.A4;
            pdfGenerator.Margins = new PageMargins { Top = 10, Bottom = 4, Left = 5, Right = 5 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {printDate}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }




        public byte[] A4_PortraitPDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            // Set some optional parameters (if needed)
            pdfGenerator.Orientation = PageOrientation.Portrait;
            pdfGenerator.Size = PageSize.A4;
            pdfGenerator.Margins = new PageMargins { Top = 10, Bottom = 4, Left = 20, Right = 20 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }
        public byte[] A4_LandscapePDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            // Set some optional parameters (if needed)
            pdfGenerator.Orientation = PageOrientation.Landscape;
            pdfGenerator.Size = PageSize.A4;
            pdfGenerator.Margins = new PageMargins { Top = 10, Bottom = 4, Left = 12, Right = 12 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:500px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:450px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }

        public byte[] Legal_PortraitPDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Portrait;
            pdfGenerator.Size = PageSize.Legal;
            pdfGenerator.Margins = new PageMargins { Top = 3, Bottom = 8, Left = 6, Right = 6 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }

        public byte[] Legal_LandscapePDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Landscape;
            pdfGenerator.Size = PageSize.Legal;
            pdfGenerator.Margins = new PageMargins { Top = 5, Bottom = 3, Left = 2, Right = 2 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }
        public byte[] GeneratePDFVoucher(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Portrait;
            pdfGenerator.Size = PageSize.A4;
            pdfGenerator.Margins = new PageMargins { Top = 14, Bottom = 9, Left = 14, Right = 14 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }

        public byte[] A3_LandscapePDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Landscape;
            pdfGenerator.Size = PageSize.A3;
            pdfGenerator.Margins = new PageMargins { Top = 5, Bottom = 5, Left = 2, Right = 2 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }
        public byte[] A2_LandscapePDF(string url, string username)
        {
            var pdfGenerator = new HtmlToPdfConverter();

            pdfGenerator.Orientation = PageOrientation.Landscape;
            pdfGenerator.Size = PageSize.A2;
            pdfGenerator.Margins = new PageMargins { Top = 5, Bottom = 5, Left = 2, Right = 2 };
            pdfGenerator.PageFooterHtml = $"<div class='col-sm-12' style='font-size: 10px;'><span class='printTime col-sm-4' style=\"text-align:left !important;margin-right:250px;\">Print At: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</span> <span class='printBy col-sm-4' style=\"margin-right:200px;\">Print By: {username}</span><span style=\"margin-left:20px\">Page <b class='page'></b> of <b class='topage'></b></span></div>";

            // Generate the PDF from the HTML content
            var pdfBytes = pdfGenerator.GeneratePdfFromFile(url, null);
            return pdfBytes;
        }
    }
}

