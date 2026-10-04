using System;
using System.Reflection;
using MediaBrowser.Controller.Entities;

class Program
{
    static void Main()
    {
        foreach (var prop in typeof(Book).GetProperties())
        {
            Console.WriteLine(prop.Name + " - " + prop.PropertyType.Name);
        }
    }
}
