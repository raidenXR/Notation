using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Notation;


namespace NotationViewer;

public partial class MainWindow : Window
{
    MemoryStream memory_stream = new(8 * 1024);
    
    public MainWindow()
    {
        InitializeComponent();
        
        hyperlink_button.Click += OpenBrowser_Click;
        input_box.KeyUp += KeyEnter_Command;        
        
        img_control.Source = new Bitmap("images/simple_str.png");
    }

    private void OpenBrowser_Click(object? sender, RoutedEventArgs e) 
    {
        const string katex_url = "https://katex.org/";
        OpenBrowser(katex_url);
    }

    private void KeyEnter_Command(object? sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && input_box.Text != null) 
        {            
            // canvas.RenderNotation(input_box.Text!);

            memory_stream.Position = 0;

            var parser = new Parser(input_box.Text);
            var hlist0 = parser.Parse().ToList();            

            using var renderer = new TeXRenderer(hlist0, 20f);
            renderer.TypesetRootHList(new System.Numerics.Vector2(30f, 30f));	
            renderer.Print();	
            renderer.Render(memory_stream);

            // Typesetting.NotationSample(input_box.Text!, memory_stream);
            memory_stream.Position = 0;
            img_control.Source = new Bitmap(memory_stream);
        }
    }    
    
    public static void OpenBrowser(string url)    
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else
        {
            // ...
        }
    }
    
}

public class ContentConverter : IValueConverter 
{
    public static readonly ContentConverter Instance = new();

    private MemoryStream ms = new(8 * 1024);

    public ContentConverter()
    {
        using var fs = File.Open("images/simple_str.png", FileMode.Open);
        fs.CopyTo(ms);
    }

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo cultureInfo)
    {
        if(value is string latex)
        {
            if(targetType.IsAssignableTo(typeof(Avalonia.Media.IImage)))
            {
                ms.Position = 0;
                return new Avalonia.Media.Imaging.Bitmap(ms);                
            }
            else if(targetType.IsAssignableTo(typeof(string)))
            {
                return latex;
            }
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }    

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo cultureInfo) 
    {
        throw new NotSupportedException();    
    }
}
