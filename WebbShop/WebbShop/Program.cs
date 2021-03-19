using WebbShop.Helpers;

namespace WebbShop
{
    internal static class Program
    {
        /// <summary>
        /// First method to be called.
        /// </summary>
        private static void Main()
        {
            Seeder.Seed();
            Menu.MainMenu();
        }
    }
}
