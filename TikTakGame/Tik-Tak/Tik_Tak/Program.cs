using System;
using System.Windows.Forms;

namespace Tik_Tak
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            string ip;
            int port;
            using( Forms.Form1 form = new Forms.Form1())
            {
                Application.Run(form);
                ip = form.ip;
                port = form.port;
            }

            using (Game1 game = new Game1())
            {
                game.IP = ip;
                game.PORT = port;
                game.Run();
            }
        }
    }
#endif
}

