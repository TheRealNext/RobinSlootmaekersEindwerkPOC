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
            string remoteFilePath = "/1//Incoming/FTP/15mb.xml";
            string remoteFilePathSFTP = "../../Incoming/SFTP/15mb.xml";
            string remoteFilePathFTPS = "1/Incoming/FTPS/15mb.xml";
            string server = "4.211.82.81";
            string[] filePaths = {
                "C:\\Users\\sloot\\Downloads\\1mb.xml",
                "C:\\Users\\sloot\\Downloads\\5mb.xml",
                "C:\\Users\\sloot\\Downloads\\15mb.xml",
                "C:\\Users\\sloot\\Downloads\\30mb.xml"
            };
            string usernameFTP = "User";
            string passwordFTP = "test";
            string usernameSFTP = "Robin";
            string passwordSFTP = "Password123!";
            double totalFTPTime = 0;
            double totalSFTPTime = 0;
            double totalFTPSTime = 0;
            double totalAS2Time = 0;

            foreach (string localFilePath in filePaths)
            {
                double fileFTPTime = 0;
                double fileSFTPTime = 0;
                double fileFTPSTime = 0;
                double fileAS2Time = 0;

                for (int i = 0; i < 3; i++)
                {
                    totalFTPTime += TestFtpSpeed(server, usernameFTP, passwordFTP, localFilePath, remoteFilePath);
                    Console.WriteLine("FTP Test: " + (i + 1));
                    totalSFTPTime += TestSftpSpeed(server, usernameSFTP, passwordSFTP, localFilePath, remoteFilePathSFTP);
                    Console.WriteLine("SFTP Test: " + (i + 1));
                    totalFTPSTime += TestFtpsSpeed(server, usernameFTP, passwordFTP, localFilePath, remoteFilePathFTPS);
                    Console.WriteLine("FTPS Test: " + (i + 1));
                    totalAS2Time += UploadAS2(localFilePath);
                    Console.WriteLine("AS2 Test: " + (i + 1));
                }
                totalFTPTime += fileFTPTime;
                totalSFTPTime += fileSFTPTime;
                totalFTPSTime += fileFTPSTime;
                totalAS2Time += fileAS2Time;

            }

            Console.WriteLine(" time in ms for FTP " + totalFTPTime);
            Console.WriteLine(" time in ms for SFTP " + totalSFTPTime);
            Console.WriteLine(" time in ms for FTPS " + totalFTPSTime);
            Console.WriteLine(" time in ms for AS2 " + totalAS2Time);
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
        public static double UploadAS2(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file does not exist.", filePath);
            }

            string url = "http://4.211.82.81:8080/as2/HttpReceiver"; // AS2 server URL
            string as2To = "mycompanyAS2"; // Replace with actual AS2-To ID
            string as2From = "as2test"; // Replace with actual AS2-From ID
            string messageId = Guid.NewGuid().ToString();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("AS2-To", as2To);
                client.DefaultRequestHeaders.Add("AS2-From", as2From);
                client.DefaultRequestHeaders.Add("Message-ID", messageId);

                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath); // Synchronous file read

                    var fileContent = new ByteArrayContent(fileBytes)
                    {
                        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml") }
                    };

                    form.Add(fileContent, "file", Path.GetFileName(filePath));

                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        HttpResponseMessage response = client.PostAsync(url, form).Result; // Synchronous wait
                        response.EnsureSuccessStatusCode();

                        var responseContent = response.Content.ReadAsStringAsync().Result; // Synchronous wait

                        stopwatch.Stop();

                        Console.WriteLine("AS2 Upload successful.");
                        Console.WriteLine($"Upload complete. Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

                        return stopwatch.Elapsed.TotalMilliseconds;
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine("An error occurred while uploading the file.");
                        Console.WriteLine(e.Message);
                        return 0; // Or handle it as needed
                    }
                }
            }
        }
    }
}