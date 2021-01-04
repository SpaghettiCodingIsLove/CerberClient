using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CerberClient.Model
{
    public static class UserData
    {
        public static string userLogin = "";
        public static string userImage = "";

        public static Bitmap ConvertStringToBitmap(string imageAsString)
        {
            byte[] byteBuffer = Convert.FromBase64String(imageAsString);
            MemoryStream memoryStream = new MemoryStream(byteBuffer);
            memoryStream.Position = 0;
            Bitmap bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);
            memoryStream.Close();

            return bmpReturn;
        }
    }
}
