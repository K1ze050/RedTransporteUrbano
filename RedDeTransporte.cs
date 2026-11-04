using System;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Inicia la aplicación corriendo la interfaz gráfica
            Application.Run(new FormPrincipal()); 
        }
    }
}