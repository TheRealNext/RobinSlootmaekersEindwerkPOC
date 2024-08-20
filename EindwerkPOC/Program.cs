using FluentFTP;
using System.Diagnostics;
using System.Net;
using Renci.SshNet;


namespace EindwerkPOC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string localFilePath = "C:\\Users\\sloot\\Downloads\\15mb.xml"; 
            string remoteFilePath = "/1//Incoming/FTP/15mb.xml"; 
            string remoteFilePathSFTP = "../../Incoming/SFTP/15mb.xml";
            string remoteFilePathFTPS = "1/Incoming/FTPS/15mb.xml";
            string server = "4.211.82.81";

            string usernameFTP = "User";
            string passwordFTP = "test";
            string usernameSFTP = "Robin";
            string passwordSFTP = "Password123!";
            double timeFTP = 0;
            double timeSFTP = 0;
            double timeFTPS = 0;
            double timeHTTPS = 0;


            for (int i = 0; i < 10; i++)
            {
                timeFTP += TestFtpSpeed(server, usernameFTP, passwordFTP, localFilePath, remoteFilePath);
                Console.WriteLine("FTP Test: " + (i + 1));
            }
            timeFTP /= 10;

            for (int i = 0; i < 10; i++)
            {
                timeSFTP += TestSftpSpeed(server, usernameSFTP, passwordSFTP, localFilePath, remoteFilePathSFTP);
                Console.WriteLine("SFTP Test: " + (i + 1));
            }
            timeSFTP /= 10;
            
            for (int i = 0; i < 10; i++)
            {
                timeFTPS += TestFtpsSpeed(server, usernameFTP, passwordFTP, localFilePath, remoteFilePathFTPS);
                Console.WriteLine("FTPS Test: " + (i + 1));
            }
            timeFTPS /= 10;
           
            for (int i = 0; i < 10; i++)
            {
                timeHTTPS += await UploadHTTPS("C:\\Users\\sloot\\Downloads\\15mb.xml");
                Console.WriteLine("HTTPS Test: " + (i + 1));
            }
            timeHTTPS /= 10;
            Console.WriteLine("Average time in ms for FTP " + timeFTP);
            Console.WriteLine("Average time in ms for SFTP " + timeSFTP);
            Console.WriteLine("Average time in ms for FTPS " + timeFTPS);
            Console.WriteLine("Average time in ms for HTTPS " + timeHTTPS);
        }

        static double TestFtpSpeed(string server, string usernameFTP, string passwordFTP, string localFilePath, string remoteFilePath)
        {
            using (var client = new FtpClient(server))
            {
                client.Credentials = new NetworkCredential(usernameFTP, passwordFTP);
                client.Connect();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                client.UploadFile(localFilePath, remoteFilePath);

                stopwatch.Stop();
                TimeSpan uploadTime = stopwatch.Elapsed;
                Console.WriteLine($"FTP Upload complete. Time taken: {uploadTime.TotalSeconds} seconds");
                client.Disconnect();
                return uploadTime.TotalMilliseconds;
            }
        }

        static double TestSftpSpeed(string server, string username, string password, string localFilePath, string remoteFilePath)
        {
            using (var client = new SftpClient(server, 22, username, password))
            {
                client.Connect();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                using (var fileStream = new FileStream(localFilePath, FileMode.Open))
                {
                    client.UploadFile(fileStream, remoteFilePath);
                }

                TimeSpan uploadTime = stopwatch.Elapsed;
                Console.WriteLine($"SFTP Upload complete. Time taken: {uploadTime.TotalSeconds} seconds");
                client.Disconnect();
                return uploadTime.TotalMilliseconds;
            }
        }

        static double TestFtpsSpeed(string ftpServer, string ftpUsername, string ftpPassword, string localFilePath, string remoteDirectory)
        {
            FtpClient client = new FtpClient(ftpServer, ftpUsername, ftpPassword);

            client.EncryptionMode = FtpEncryptionMode.Explicit; 
            client.ValidateCertificate += (control, e) => e.Accept = true; 

            client.Connect();

            string fileName = Path.GetFileName(localFilePath);


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            using (FileStream fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                client.Upload(fs, remoteDirectory);
            }

            stopwatch.Stop();
            TimeSpan uploadTime = stopwatch.Elapsed;
            Console.WriteLine($"FTPS Upload complete. Time taken: {uploadTime.TotalSeconds} seconds");
            client.Disconnect();
            return uploadTime.TotalMilliseconds;

        }

        public static async Task<Double> UploadHTTPS(string filePath)
        {
            double time = 0;
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file does not exist.", filePath);
            }

            using (HttpClient client = new HttpClient())
            {
                using (MultipartFormDataContent form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                    ByteArrayContent fileContent = new ByteArrayContent(fileBytes);

                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    form.Add(fileContent, "file", Path.GetFileName(filePath));
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        HttpResponseMessage response = await client.PostAsync("https://httpbin.org/post", form);

                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync();

                        stopwatch.Stop();

                        TimeSpan uploadTime = stopwatch.Elapsed;
                        time = uploadTime.TotalMilliseconds;
                        Console.WriteLine("Success");
                        Console.WriteLine($"Upload complete. Time taken: {uploadTime.TotalSeconds:F2} seconds");
                        return time;

                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("An error occurred while uploading the file.");
                        Console.WriteLine(e.Message);
                    }
                }
                return time;
            }
        }
    }
}