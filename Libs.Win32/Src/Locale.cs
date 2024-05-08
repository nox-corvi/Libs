using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Nox.Win32;

public class Language
{
    public string Name { get; private set; }
    public string Code { get; private set; }

    protected static List<Language> _languages = new();

    public Language(string Code, string Name)
    {
        this.Code = Code;
        this.Name = Name;

        _languages.Add(this);
    }

    public static IEnumerable<Language> List() => _languages;

    // Standard Sprachen
    public static readonly Language English = new("EN", "English");
    public static readonly Language German = new("DE", "German");
    // Weitere standard Sprachen können hier hinzugefügt werden
}

public class LocaleKey
{
    public string Code { get; private set; }
    public string DefaultText { get; private set; }

    protected static List<LocaleKey> _keys = new();

    public LocaleKey(string code, string defaultText)
    {
        DefaultText = DefaultText;
        Code = code;

        _keys.Add(this);
    }

    public static IEnumerable<LocaleKey> List() => _keys;


    public static readonly LocaleKey BUTTON_OK = new(nameof(BUTTON_OK), "Ok");
    public static readonly LocaleKey BUTTON_CANCEL = new(nameof(BUTTON_CANCEL), "Cancel");

    public static readonly LocaleKey BUTTON_YES = new(nameof(BUTTON_YES), "Yes");
    public static readonly LocaleKey BUTTON_NO = new(nameof(BUTTON_NO), "No");
}

public class LocaleService 
{
   // protected static LocaleService _self;
    protected Language _CurrentLanguage = Language.English;

    public static Language CurrentLanguage { get; set; }// => _self._CurrentLanguage; set => _self._CurrentLanguage = value; }
    
    public virtual string GetLocalizedString(LocaleKey localKey)
        => localKey.Code switch
        {
            nameof(LocaleKey.BUTTON_OK) => CurrentLanguage.Code switch
            {
                // keine sprachlichen Unterschiede vorhanden ...
                _ => "Ok"
            },
            nameof(LocaleKey.BUTTON_CANCEL) => CurrentLanguage.Code switch
            {
                "DE" => "Abbrechen",
                _ => "Cancel"
            },

            nameof(LocaleKey.BUTTON_YES) => CurrentLanguage.Code switch
            {
                "DE" => "Ja",
                _ => "Yes"
            },
            nameof(LocaleKey.BUTTON_NO) => CurrentLanguage.Code switch
            {
                "DE" => "Nein",
                _ => "No"
            },
            _ => "<not localized>"
        };

    //public static string GetCurrentLocalizedString(LocalKey localKey)
    //    => _self.GetLocalizedString(localKey);

    //static LocaleService() =>
    //    _self = new LocaleService();
}
