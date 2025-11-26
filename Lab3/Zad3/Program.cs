using System;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ManagedCuda;
using ManagedCuda.VectorTypes;
using System.Runtime.InteropServices;

namespace Zad3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string serverIP = "127.0.0.1";
            int port = 2040;

            TcpClient client = new TcpClient(serverIP, port);
            NetworkStream stream = client.GetStream();

            byte[] lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);
            int length = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

            byte[] receivedData = new byte[length];
            int bytesRead = 0;
            while (bytesRead < length)
            {
                bytesRead += stream.Read(receivedData, bytesRead, length - bytesRead);
            }

            Bitmap fragment;
            using (MemoryStream ms = new MemoryStream(receivedData))
            {
                fragment = new Bitmap(ms);
            }

            Bitmap processed;
            if (IsCudaAvailable())
            {
                Console.WriteLine("CUDA dostępna, używam GPU...");
                processed = ApplySobelGPU(fragment);
            }
            else
            {
                Console.WriteLine("Brak GPU, używam CPU...");
                processed = ApplySobel(fragment);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                processed.Save(ms, ImageFormat.Png);
                byte[] sendData = ms.ToArray();

                int size = System.Net.IPAddress.HostToNetworkOrder(sendData.Length);
                stream.Write(BitConverter.GetBytes(size), 0, 4);
                stream.Write(sendData, 0, sendData.Length);
            }

            client.Close();
            Console.WriteLine("Fragment przetworzony i wysłany do serwera.");
        }

        static bool IsCudaAvailable()
        {
            try
            {
                if (!System.IO.File.Exists(@"C:\Windows\System32\nvcuda.dll"))
                    return false; // brak sterownika CUDA

                int deviceCount = CudaContext.GetDeviceCount();
                return deviceCount > 0;
            }
            catch (CudaException)
            {
                return false;
            }
        }

        static Bitmap ApplySobelGPU(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            if (!IsCudaAvailable())
                return ApplySobel(image);

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int stride = bmpData.Stride;
            int bytes = stride * height;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);
            image.UnlockBits(bmpData);

            byte[] gray = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * stride + x * 3;
                    gray[y * width + x] = (byte)((rgbValues[idx] + rgbValues[idx + 1] + rgbValues[idx + 2]) / 3);
                }
            }

            using (CudaContext ctx = new CudaContext())
            using (CudaDeviceVariable<byte> dGray = new CudaDeviceVariable<byte>(width * height))
            using (CudaDeviceVariable<byte> dResult = new CudaDeviceVariable<byte>(width * height))
            {
                dGray.CopyToDevice(gray);

                CudaKernel kernel = ctx.LoadKernel("Sobel.ptx", "SobelFilter");
                kernel.BlockDimensions = new dim3(16, 16, 1);
                kernel.GridDimensions = new dim3((width + 15) / 16, (height + 15) / 16, 1);

                kernel.Run(dGray.DevicePointer, dResult.DevicePointer, width, height);

                byte[] result = new byte[width * height];
                dResult.CopyToHost(result);

                Bitmap output = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                BitmapData outData = output.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                int outStride = outData.Stride;
                byte[] outValues = new byte[outStride * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte val = result[y * width + x];
                        int idx = y * outStride + x * 3;
                        outValues[idx] = val;
                        outValues[idx + 1] = val;
                        outValues[idx + 2] = val;
                    }
                }

                Marshal.Copy(outValues, 0, outData.Scan0, outValues.Length);
                output.UnlockBits(outData);

                return output;
            }
        }

        static Bitmap ApplySobel(Bitmap image)
        {
            Bitmap gray = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color c = image.GetPixel(x, y);
                    int g = (c.R + c.G + c.B) / 3;
                    gray.SetPixel(x, y, Color.FromArgb(g, g, g));
                }
            }

            Bitmap result = new Bitmap(image.Width, image.Height);

            int[,] GX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] GY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    int sx = 0, sy = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int pixelValue = gray.GetPixel(x + kx, y + ky).R;
                            sx += pixelValue * GX[ky + 1, kx + 1];
                            sy += pixelValue * GY[ky + 1, kx + 1];
                        }
                    }

                    int mag = Math.Min(255, (int)Math.Sqrt(sx * sx + sy * sy));
                    result.SetPixel(x, y, Color.FromArgb(mag, mag, mag));
                }
            }

            return result;
        }
    }
}
