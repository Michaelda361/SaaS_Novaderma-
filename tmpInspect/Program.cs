using System;
using System.Linq;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.Load("PdfSharpCore");
        var type = asm.GetType("PdfSharpCore.Pdf.AcroForms.PdfTextField");
        Console.WriteLine("Constructors:");
        foreach (var c in type.GetConstructors()) {
            Console.WriteLine(" - " + string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)));
        }
        var form = asm.GetType("PdfSharpCore.Pdf.AcroForms.PdfAcroForm");
        Console.WriteLine(form.FullName);
        Console.WriteLine("Form Constructors:");
        foreach (var c in form.GetConstructors()) {
            Console.WriteLine(" - " + string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)));
        }
    }
}
