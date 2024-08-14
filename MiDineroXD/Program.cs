using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Newtonsoft.Json.Linq;
using System.Globalization;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine("Por favor, ingrese la ruta de la carpeta:");
        string folderPath = Console.ReadLine();

        if (string.IsNullOrEmpty(folderPath))
        {
            //Console.WriteLine("La ruta no puede estar vacía.");
            folderPath = "C:\\Users\\lberonne\\Downloads\\Documentos";
            //return;
        }

        if (!System.IO.Directory.Exists(folderPath))
        {
            Console.WriteLine("La ruta especificada no existe.");
            return;
        }

        Console.WriteLine($"La ruta de la carpeta es: {folderPath}");

        List<(string filePath, DateTime fecha)> pdfFilesWithDates = new List<(string, DateTime)>();

        string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");

        foreach (string pdfPath in pdfFiles)
        {
            using (PdfDocument document = PdfDocument.Open(pdfPath))
            {
                foreach (Page page in document.GetPages())
                {
                    string text = page.Text;

                    string periodoAbonado = ExtractField(text, "PeríodoAbonado:", "NúmeroLegajo");
                    DateTime fecha = ConvertirCadenaADateTime(periodoAbonado);

                    pdfFilesWithDates.Add((pdfPath, fecha));
                }
            }
        }

        var sortedPdfFiles = pdfFilesWithDates.OrderByDescending(p => p.fecha).ToList();

        foreach (var item in sortedPdfFiles)
        {
            Console.WriteLine($"Procesando archivo: {Path.GetFileName(item.filePath)} con fecha {item.fecha:dd/MM/yyyy}");

            using (PdfDocument document = PdfDocument.Open(item.filePath))
            {
                foreach (Page page in document.GetPages())
                {
                    string text = page.Text;

                    string periodoAbonado = ExtractField(text, "PeríodoAbonado:", "NúmeroLegajo");
                    decimal valorDolar = ExtractDolarValue(item.fecha).Result;

                    string totalNeto = ExtractField(text, "TotalNETO:", "TotalBruto");
                    Console.WriteLine("Total NETO: " + totalNeto);

                    string valorSinPunto = totalNeto.Replace(".", "").Split(',')[0];
                    decimal valorDecimal = decimal.Parse(valorSinPunto);
                    decimal totalDolares = 0;
                    if (valorDolar > 0) {
                        totalDolares = valorDecimal / valorDolar;
                    }
                    else
                    {
                        totalDolares = 0;
                    }

                    Console.WriteLine("Total en Dólares: " + totalDolares);
                    Console.WriteLine(); 
                }
            }
        }

        Console.ReadLine();
    }

    static string ExtractField(string text, string start, string end)
    {
        int startIndex = text.IndexOf(start);
        if (startIndex == -1)
        {
            return "Campo no encontrado";
        }

        startIndex += start.Length;
        int endIndex = text.IndexOf(end, startIndex);
        if (endIndex == -1)
        {
            endIndex = text.Length;
        }

        return text.Substring(startIndex, endIndex - startIndex).Trim();
    }

    static async Task<decimal> ExtractDolarValue(DateTime fecha)
    {
        string apiUrl = "https://api.argentinadatos.com/v1/cotizaciones/dolares";
        decimal valorDolarBlueCompra = 0;

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            JArray jsonArray = JArray.Parse(responseBody);

            foreach (var item in jsonArray)
            {
                DateTime fechaJson = DateTime.Parse(item["fecha"].ToString());
                if (item["casa"].ToString() == "blue" && fechaJson.Date == fecha.Date)
                {
                    valorDolarBlueCompra = item["compra"].ToObject<decimal>();
                    break;
                }
            }
        }

        return valorDolarBlueCompra;
    }


    static DateTime ConvertirCadenaADateTime(string periodo)
    {
        try
        {
            string formato = "MMMMyyyy";
            DateTime fecha = DateTime.ParseExact(periodo, formato, new CultureInfo("es-ES"));

            fecha = new DateTime(fecha.Year, fecha.Month, 1);

            return fecha;
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }
}
