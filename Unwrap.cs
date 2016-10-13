using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Author: Alexander S. Taschilin

namespace oradev
{
    public class Unwrap
    {
        private const string SUB_FROM =
          "3D6585B318DBE287F152AB634BB5A05F7D687B9B24C228678ADEA4261E03EB176F343E7A3FD2A96A0FE935561FB14D1078D975F6BC4104816106F9ADD6D5297E" +
          "869E79E505BA84CC6E278EB05DA8F39FD0A271B858DD2C38994C480755E4538C46B62DA5AF322240DC50C3A1258B9C16605CCFFD0C981CD4376D3C3A30E86C31" +
          "47F533DA43C8E35E1994ECE6A39514E09D64FA5915C52FCABB0BDFF297BF0A76B449445A1DF0009621807F1A82394FC1A7D70DD1D8FF139370EE5BEFBE09B977" +
          "72E7B254B72AC7739066200E51EDF87C8F2EF412C62B83CDACCB3BC44EC069366202AE88FCAA4208A64557D39ABDE1238D924A1189746B91FBFEC901EA1BF7CE";

        private const int HEADER_LINES = 20;
        private const int LINES_LENGTH = 72;
        private const int SHA1_DIGEST_LENGTH = 40;

        private byte[] SUB_TO_RAW = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
        private byte[] SUB_FROM_RAW = Enumerable.Range(0, SUB_FROM.Length).Where(x => x % 2 == 0)
                                                .Select(x => Convert.ToByte(SUB_FROM.Substring(x, 2), 16)).ToArray();

        public Unwrap() { }

        public string Do(string input, Encoding enc)
        {
            try
            {
                string[] src = input.Replace("\n", "").Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (src.Count() < 2 || src[0].ToUpper().IndexOf("WRAPPED") < 0) return string.Empty;

                byte[] buffer = { };
                int theEndIndex = 0;
                for (int i = 0; i < src.Count(); i++)
                    if (i >= HEADER_LINES)
                    {
                        if (src[i].Trim().StartsWith("/") && src[i].Trim().Length != LINES_LENGTH)
                        {
                            theEndIndex = i;
                            break;
                        }
                        byte[] buf = Convert.FromBase64String(src[i].Trim());

                        if (i == HEADER_LINES)
                            buf = HexToByte(ByteToHex(buf).Substring(SHA1_DIGEST_LENGTH));

                        int placement = buffer.Length;
                        Array.Resize<byte>(ref buffer, buffer.Length + buf.Length);
                        buf.CopyTo(buffer, placement);
                    }

                buffer = Translate(buffer);
                StringBuilder sb = new StringBuilder("CREATE OR REPLACE ");
                using (var streamReader = new StreamReader(new DeflateStream(new MemoryStream(buffer, 2, buffer.Length - 2),
                                                           CompressionMode.Decompress), enc))
                {
                    sb.Append(streamReader.ReadToEnd());
                }
                for (int i = theEndIndex; i < src.Count(); i++) sb.Append("\r" + src[i].Trim());
                return sb.ToString();
            }
            catch { return string.Empty; }
        }

        private static string ByteToHex(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        private static byte[] HexToByte(string data)
        {
            return Enumerable.Range(0, data.Length).Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(data.Substring(x, 2), 16)).ToArray();
        }

        private byte[] Translate(byte[] data)
        {
            return data.Select((x, i) => { return SUB_FROM_RAW[Array.FindIndex(SUB_TO_RAW, b => b == data[i])]; }).ToArray();
        }
    }
}
