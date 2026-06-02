using System;
using System.Linq;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.LoadFrom("C:\\Users\\Laboratorios\\.nuget\\packages\\pdfsharpcore\\1.3.4\\lib\\netstandard2.0\\PdfSharpCore.dll");
        var types = asm.GetTypes().Where(t => t.Name.Contains("TextField") || t.Name.Contains("Acro")).OrderBy(t => t.FullName);
        foreach (var type in types) Console.WriteLine(type.FullName);
    }
}
