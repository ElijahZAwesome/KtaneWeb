﻿using System;
using System.Linq;
using System.Reflection;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse mainPage(HttpRequest req, KtaneWebConfig config)
        {
            // Access keys:
            // A
            // B    difficulty: very hard for both
            // C
            // D    difficulty: very hard for defuser
            // E    difficulty: easy
            // F
            // G
            // H    difficulty: hard
            // I    difficulty: medium
            // J    JSON
            // K
            // L
            // M    Manual
            // N    Needy
            // O    Mods
            // P
            // Q
            // R    Regular
            // S    Show missing
            // T    Tutorial video
            // U    Source code
            // V    Vanilla
            // W    Steam Workshop Item
            // X    difficulty: very hard for expert
            // Y    difficulty: very easy
            // Z

            var sheets = config.KtaneModules.ToDictionary(mod => mod.Name, mod => config.EnumerateSheetUrls(mod.Name, config.KtaneModules.Select(m => m.Name).Where(m => m != mod.Name && m.StartsWith(mod.Name)).ToArray()));

            var selectables = Ut.NewArray(
                new Selectable
                {
                    HumanReadable = "Manual",
                    Accel = 'M',
                    Icon = mod => new IMG { class_ = "icon manual-icon", title = "Manual", alt = "Manual", src = sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["icon"].GetString() : null },
                    DataAttributeName = "manual",
                    DataAttributeValue = mod => sheets.Get(mod.Name, null)?.ToString(),
                    Url = mod => sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["url"].GetString() : null,
                    ShowIcon = mod => sheets[mod.Name].Count > 0,
                    CssClass = "manual"
                },
                new Selectable
                {
                    HumanReadable = "Steam Workshop Item",
                    Accel = 'W',
                    Icon = mod => new IMG { class_ = "icon", title = "Steam Workshop Item", alt = "Steam Workshop Item", src = config.SteamIconUrl },
                    DataAttributeName = "steam",
                    DataAttributeValue = mod => mod.SteamID?.Apply(s => $"http://steamcommunity.com/sharedfiles/filedetails/?id={s}"),
                    Url = mod => $"http://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamID}",
                    ShowIcon = mod => mod.SteamID != null
                },
                new Selectable
                {
                    HumanReadable = "Source code",
                    Accel = 'u',
                    Icon = mod => new IMG { class_ = "icon", title = "Source code", alt = "Source code", src = config.UnityIconUrl },
                    DataAttributeName = "source",
                    DataAttributeValue = mod => mod.SourceUrl,
                    Url = mod => mod.SourceUrl,
                    ShowIcon = mod => mod.SourceUrl != null
                },
                new Selectable
                {
                    HumanReadable = "Tutorial video",
                    Accel = 'T',
                    Icon = mod => new IMG { class_ = "icon", title = "Tutorial video", alt = "Tutorial video", src = config.TutorialVideoIconUrl },
                    DataAttributeName = "video",
                    DataAttributeValue = mod => mod.TutorialVideoUrl,
                    Url = mod => mod.TutorialVideoUrl,
                    ShowIcon = mod => mod.TutorialVideoUrl != null
                });

            var filters = Ut.NewArray(
                new KtaneFilter(typeof(KtaneModuleType), mod => mod.Type),
                new KtaneFilter(typeof(KtaneModuleOrigin), mod => mod.Origin),
                new KtaneFilter(typeof(KtaneModuleDifficulty), mod => mod.Difficulty)
            );

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = "//fonts.googleapis.com/css?family=Special+Elite", rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js" },
                    new SCRIPTLiteral($@"
                        Ktane = {{
                            Filters: {filters.Select(f => new JsonDict {
                                { "id", f.DataAttributeName },
                                { "values", Enum.GetValues(f.EnumType).Cast<Enum>().Select(e => new { Value = e, Attr = e.GetCustomAttribute<KtaneFilterOptionAttribute>() }).Where(inf => inf.Attr != null).Select(inf => inf.Value.ToString()).ToJsonList() },
                                { "alwaysShow", Enum.GetValues(f.EnumType).Cast<Enum>().Select(e => new { Value = e, Attr = e.GetCustomAttribute<KtaneFilterOptionAttribute>() }).Where(inf => inf.Attr == null).Select(inf => inf.Value.ToString()).ToJsonList() }
                            }).ToJsonList()},
                            Selectables: {selectables.Select(s => s.DataAttributeName).ToJsonList()}
                        }};
                    "),
                    new SCRIPT { src = req.Url.WithParent("js").ToHref() },
                    new META { name = "viewport", content = "width=device-width" }),
                new BODY(
                    new DIV { id = "main-content" }._(
                        filters
                            .Select(filter => new { Type = filter.EnumType, Attr = filter.EnumType.GetCustomAttributes<KtaneFilterAttribute>().FirstOrDefault() })
                            .Where(filter => filter.Attr != null)
                            .Select(filterInf =>
                                new DIV { class_ = "filter-section" }._(
                                    new DIV { class_ = "filter-sub" }._(filterInf.Attr.FilterName, ":"),
                                    Enum.GetValues(filterInf.Type).Cast<Enum>()
                                        .Select(val => new { Value = val, Attr = val.GetCustomAttribute<KtaneFilterOptionAttribute>() })
                                        .Where(fv => fv.Attr != null)
                                        .Select(fv => new DIV(
                                            new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-" + fv.Value.ToString() }, " ",
                                            new LABEL { for_ = "filter-" + fv.Value.ToString(), accesskey = fv.Attr.Accel.ToString().ToLowerInvariant() }._(fv.Attr.ReadableName.Accel(fv.Attr.Accel))))))
                            .ToArray()
                            .Apply(filterUis =>
                                new TABLE { class_ = "header" }._(
                                    new TR(new TD { rowspan = 2 }._(new IMG { class_ = "logo", src = config.LogoUrl }), new TH { class_ = "links-head" }._("Make links go to:"), new TH { colspan = 2, class_ = "filters-head" }._("Filters:")),
                                    new TR(
                                        new TD { class_ = "selectables" }._(
                                            selectables.Select(sel => new DIV(
                                                new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.DataAttributeName}" }.Data("selectable", sel.DataAttributeName), " ",
                                                new LABEL { class_ = "set-selectable", id = $"selectable-label-{sel.DataAttributeName}", for_ = $"selectable-{sel.DataAttributeName}", accesskey = sel.Accel.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel)))),
                                            new DIV(
                                                new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-show-missing" }, " ",
                                                new LABEL { for_ = "filter-show-missing", accesskey = "s" }._("Show missing".Accel('S')))),
                                        new TD { class_ = "filters-1" }._(new DIV(filterUis[0]), new DIV(filterUis[1])),
                                        new TD { class_ = "filters-2" }._(filterUis[2])))),
                        new TABLE { class_ = "main-table" }._(
                            new TR(
                                new TH { colspan = selectables.Length }._("Links"),
                                new TH("Name"),
                                new TH("Information")),
                            config.KtaneModules.Select(mod =>
                                new TR { class_ = "mod" }
                                    .Data("mod", mod.Name)
                                    .AddData(selectables, sel => sel.DataAttributeName, sel => sel.DataAttributeValue(mod))
                                    .AddData(filters, flt => flt.DataAttributeName, flt => flt.GetValue(mod).ToString())
                                    ._(
                                        selectables.Select((sel, ix) => new TD { class_ = "selectable" + (ix == selectables.Length - 1 ? " last" : null) }._(sel.ShowIcon(mod) ? new A { href = sel.Url(mod), class_ = sel.CssClass }._(sel.Icon(mod)) : null)),
                                        new TD(new A { class_ = "modlink" }._(mod.Icon(config), mod.Name)),
                                        new TD { class_ = "infos" }._(
                                            new DIV { class_ = "inf-author" }._(mod.Author),
                                            new DIV { class_ = "inf-type" }._(mod.Type.ToString()),
                                            new DIV { class_ = "inf-difficulty" }._(mod.Difficulty.ToReadable()))))),
                        new DIV { class_ = "links" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J'))),
                        new DIV { class_ = "credits" }._("Icons by lumbud84 and samfun123."),
                        new DIV { class_ = "extra-links" }._(
                            new H3("Controls to highlight elements in HTML manuals"),
                            new TABLE { class_ = "highlighting-controls" }._(
                                new TR(new TH("Control (Windows)"), new TH("Control (Mac)"), new TH("Function")),
                                new TR(new TD("Ctrl+Click"), new TD("Command+Click"), new TD("Highlight a table column")),
                                new TR(new TD("Shift+Click"), new TD("Shift+Click"), new TD("Highlight a table row")),
                                new TR(new TD("Alt+Click or Ctrl+Shift+Click"), new TD("Command+Shift+Click"), new TD("Highlight a table cell or highlight an item in a list"))),
                            new H3("Additional resources"),
                            new UL(
                                new LI(new A { href = "https://www.dropbox.com/s/paluom4wlogjdl0/ModsOnlyManual_Sorted_A-Z.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual (mods only)")),
                                new LI(new A { href = "https://www.dropbox.com/s/4bkfwoa4d7p0a7z/ModsOnlyManual_Sorted_A-Z_with_Cheat_Sheets.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual with cheat sheets (mods only)")),
                                new LI(new A { href = "More/Output%20Log%20Reader.html" }._("samfun123’s output log reader")),
                                new LI(new A { href = "https://docs.google.com/document/d/1zObWfLI8RMiNL1b6AXfiy4cwjGD9H3oStPiZaEOS5Lc" }._("On the Subject of Entering the World of Mods (by Rexkix)"))),
                            new H3("Default file locations"),
                            new DL(
                                new DT("Output log (Windows, Steam):"), new DD(new CODE(@"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt")),
                                new DT("Output log (Windows, Oculus):"), new DD(new CODE(@"C:\Program Files (x86)\Oculus\Software\steel-crate-games-keep-talking-and-nobody-explodes\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt")),
                                new DT("Output log (Mac):"), new DD(new CODE(@"~/Library/Logs/Unity/Player.log")),
                                new DT("Screenshots (Windows, Steam):"), new DD(new CODE(@"C:\Program Files (x86)\Steam\userdata\<some number>\760\remote\341800\screenshots")),
                                new DT("Screenshots (Mac, Steam):"), new DD(new CODE(@"~/Library/Application Support/Steam/userdata/<some number>/760/remote/341800/screenshots"))))))));
        }
    }
}
