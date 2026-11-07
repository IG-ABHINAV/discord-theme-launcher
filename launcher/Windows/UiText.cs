using System.Globalization;

namespace CommunityThemes;

// Host-facing copy. English is the source and the default. Docs stay English.
internal static partial class UiText
{
    public static string Current { get; private set; } = "en";
    public const string ServerHelpTitle = "Choose your server";
    public const string ServerHelpBody = "In Discord desktop:\n1. Open User Settings > Advanced > Developer Mode.\n2. Right-click the server icon and choose Copy Server ID.\n3. Paste it into Server ID in the editor.\n\nOr right-click a text channel, choose Copy Channel Link and paste it. Invite links cannot identify the target. You must already belong to the server. No token or credentials are needed.";
    public const string HelpTitle = "Theme Studio · Help";
    public const string HelpBody = "Choose a target in Server, then explore Style and Artwork. Click a starting preset to load its coordinated font, palette and images. The preview always starts on your target. Other server and DMs show stock Discord on purpose, demonstrating that your theme stays local to the selected server.\n\nSave or Share theme writes a .dtheme file with selected assets and the custom font license. Other members must choose to adopt it themselves. One personal theme can be active at a time. Recovery offers disable, restore and the original multi-server preset manager. Applying original presets replaces the personal slot too.\n\nWindows x64 / standard stable desktop Discord / default Vencord only. Editing is always a preview. Apply opens a review overlay showing the theme and target server. Only Continue & apply starts the operation and authorizes the Discord restart; Back to preview or Escape cancels. Missing Vencord setup is offered in that overlay with explicit consent. Recovery disable/restore use the same confirmation pattern. Vencord is unofficial and has Discord terms/account implications. This independent tool is not affiliated with Discord.\n\nBundled fonts are SIL OFL 1.1. Custom static TTF/OTF imports require editable embedding and redistribution rights plus the license. PNG/JPEG images must fit 8 MiB, 4096 px per side and 16 million pixels. Configurations are limited to 24 MiB. See the repository's Installation, Compatibility and Validation documents for details.";
    public const string WebViewMissing = "Theme Studio needs Microsoft Edge WebView2 Runtime. Install it from Microsoft's official WebView2 download page, then reopen the launcher. Your saved .dtheme files remain safe.\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/";
    public const string PreviewWebViewMissing = "The local preview needs Microsoft Edge WebView2 Runtime.\nInstall it from Microsoft's official WebView2 download page, then reopen the launcher.\nYour configuration can still be saved and shared.";

    public static bool Supported(string? code) => code is "en" or "it" or "es" or "ru" or "de";
    public static void Use(string? code) { if (Supported(code)) Current = code!; }
    public static void Load(string root)
    {
        try
        {
            var path = Path.Combine(root, "language.txt");
            if (!File.Exists(path)) return;
            var code = File.ReadAllText(path).Trim();
            Current = Supported(code) ? code : "en";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
    public static void Save(string root)
    {
        try
        {
            ThemeStore.CheckPath(root);
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "language.txt");
            ThemeStore.CheckPath(path);
            File.WriteAllText(path, Current);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }

    public static string Text(string english)
    {
        if (string.IsNullOrEmpty(english) || Current == "en") return english;
        if (Map(Current).TryGetValue(english, out var translated)) return translated;
        const string retry = " No automatic retry was made.";
        if (english.Length > retry.Length && english.EndsWith(retry, StringComparison.Ordinal))
            return Text(english[..^retry.Length]) + Text(retry);
        foreach (var (prefix, suffix, template) in Patterns)
        {
            if (!english.StartsWith(prefix, StringComparison.Ordinal) || !english.EndsWith(suffix, StringComparison.Ordinal) || english.Length <= prefix.Length + suffix.Length) continue;
            var argument = suffix.Length == 0 ? english[prefix.Length..] : english[prefix.Length..^suffix.Length];
            return string.Format(CultureInfo.InvariantCulture, Text(template), argument);
        }
        return english;
    }

    public static string Format(string english, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Text(english), args);

    private static readonly (string Prefix, string Suffix, string Template)[] Patterns =
    [
        ("Font imported with its redistribution license: ", "", "Font imported with its redistribution license: {0}"),
        ("Imported ", ". The image is embedded; the original path is not shared.", "Imported {0}. The image is embedded; the original path is not shared."),
        ("Theme applied to server ", ". Discord startup confirmed. Changes are local to you.", "Theme applied to server {0}. Discord startup confirmed. Changes are local to you."),
        ("Downloading and verifying ", "…", "Downloading and verifying {0}…"),
        ("Vencord is incomplete. Missing or unreadable: ", ". Choose Repair official Vencord before reopening Discord.", "Vencord is incomplete. Missing or unreadable: {0}. Choose Repair official Vencord before reopening Discord.")
        ,("Unsupported stylesheet rule: ", ". @media, @supports, @container and named animations are supported; global registrations and nested CSS need a reviewed adapter.", "Unsupported stylesheet rule: {0}. @media, @supports, @container and named animations are supported; global registrations and nested CSS need a reviewed adapter.")
    ];

    private static Dictionary<string, string> Map(string code) => code switch
    {
        "it" => Italian,
        "es" => Spanish,
        "ru" => Russian,
        "de" => German,
        _ => EnglishFallback
    };

    private static readonly Dictionary<string, string> EnglishFallback = [];

    private static readonly Dictionary<string, string> Italian = new()
    {
        [ServerHelpTitle] = "Scegli il server",
        [ServerHelpBody] = "In Discord desktop:\n1. Apri Impostazioni utente > Avanzate > Modalità sviluppatore.\n2. Fai clic destro sull’icona del server e scegli Copia ID server.\n3. Incollalo nel campo ID server dell’editor.\n\nOppure fai clic destro su un canale testuale, scegli Copia link del canale e incollalo. I link di invito non identificano il server. Devi già far parte del server. Non servono token o credenziali.",
        [HelpTitle] = "Studio temi · Guida",
        [HelpBody] = "Scegli un server in Server, poi esplora Stile e Grafica. Un preset iniziale carica font, tavolozza e immagini coordinati. L’anteprima parte sempre dal server scelto. Altro server e i DM mostrano Discord standard di proposito: il tema resta sul server selezionato.\n\nSalva o Condividi tema scrive un file .dtheme con gli asset scelti e la licenza del font personalizzato. Gli altri membri decidono se adottarlo. Può essere attivo un solo tema personale. Recupero offre disattivazione, ripristino e la gestione dei preset originali. Applicare i preset originali sostituisce anche lo slot personale.\n\nSolo Windows x64, Discord desktop stabile standard e Vencord predefinito. La modifica è sempre un’anteprima. Applica apre un riepilogo con tema e server. Solo Continua e applica avvia l’operazione e autorizza il riavvio di Discord; Torna all’anteprima o Esc annulla. Se Vencord manca, la stessa finestra propone la preparazione ufficiale con consenso esplicito. Disattivazione e ripristino usano la stessa conferma. Vencord non è ufficiale e ha implicazioni sui termini e sull’account Discord. Questo strumento indipendente non è affiliato a Discord.\n\nI font inclusi sono SIL OFL 1.1. I font TTF/OTF statici importati richiedono il diritto di incorporamento modificabile, di ridistribuzione e il testo della licenza. Le immagini PNG/JPEG devono stare entro 8 MiB, 4096 px per lato e 16 milioni di pixel. Le configurazioni sono limitate a 24 MiB. I dettagli sono nei documenti Installation, Compatibility e Validation del repository.",
        [WebViewMissing] = "Studio temi richiede Microsoft Edge WebView2 Runtime. Installalo dalla pagina ufficiale di Microsoft, poi riapri il launcher. I file .dtheme salvati restano al sicuro.\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
        [PreviewWebViewMissing] = "L’anteprima locale richiede Microsoft Edge WebView2 Runtime.\nInstallalo dalla pagina ufficiale di Microsoft, poi riapri il launcher.\nLa configurazione si può comunque salvare e condividere.",
        ["Theme Studio · Discord Theme Launcher — {0}"] = "Studio temi · Discord Theme Launcher — {0}",
        ["Opening Theme Studio…"] = "Apertura di Studio temi…",
        ["Theme Studio could not load. Reopen the application; your saved configurations are safe."] = "Studio temi non si è caricato. Riapri l’applicazione; le configurazioni salvate sono al sicuro.",
        ["Preparing local preview…"] = "Preparazione dell’anteprima locale…",
        ["Preview or display font could not load. Choose a supported font and rebuild the preview before applying."] = "L’anteprima o il font dei titoli non si è caricato. Scegli un font supportato e ricostruisci l’anteprima prima di applicare.",
        ["Got it"] = "Ho capito",
        ["Design here. Preview here. Apply when you're ready."] = "Progetta qui. Guarda l’anteprima qui. Applica quando sei pronto.",
        ["The last configuration could not be reopened. Open another .dtheme file or start a new one."] = "L’ultima configurazione non si è riaperta. Apri un altro file .dtheme o iniziane una nuova.",
        ["The Vencord profile could not be read. Check the manual recovery guide."] = "Il profilo Vencord non si è letto. Controlla la guida di recupero manuale.",
        ["Open configuration"] = "Apri configurazione",
        ["Save a copy"] = "Salva una copia",
        ["Help"] = "Guida",
        ["The configuration could not be processed. Check the format, size and permissions."] = "La configurazione non è stata elaborata. Controlla formato, dimensione e permessi.",
        ["Configuration action"] = "Azione sulla configurazione",
        ["Could not complete this action"] = "Impossibile completare questa azione",
        ["The action could not finish. Check the file's format, size and permissions. Your saved configurations are safe."] = "L’azione non è terminata. Controlla formato, dimensione e permessi del file. Le configurazioni salvate sono al sicuro.",
        ["Theme configuration (*.dtheme)"] = "Configurazione tema (*.dtheme)",
        ["Open a configuration and its assets"] = "Apri una configurazione e i suoi asset",
        ["Configuration opened. Review the target server before applying."] = "Configurazione aperta. Controlla il server di destinazione prima di applicare.",
        ["Share a copy with embedded assets"] = "Condividi una copia con gli asset incorporati",
        ["Save your configuration"] = "Salva la configurazione",
        ["Portable copy saved with selected assets and font license. Send it to members yourself; each member applies it locally."] = "Copia portatile salvata con gli asset scelti e la licenza del font. Inviala tu ai membri; ognuno la applica in locale.",
        ["Your configuration and selected assets are saved together."] = "La configurazione e gli asset scelti sono salvati insieme.",
        ["Images (*.png;*.jpg;*.jpeg)"] = "Immagini (*.png;*.jpg;*.jpeg)",
        ["Import an image you have permission to use and share"] = "Importa un’immagine che hai il permesso di usare e condividere",
        ["Font imported with its redistribution license: {0}"] = "Font importato con la licenza di ridistribuzione: {0}",
        ["Imported {0}. The image is embedded; the original path is not shared."] = "Importato {0}. L’immagine è incorporata; il percorso originale non viene condiviso.",
        ["Unsaved configuration"] = "Configurazione non salvata",
        ["This configuration has unsaved edits. Save it before continuing if you want to keep them."] = "Questa configurazione ha modifiche non salvate. Salvala prima di continuare se vuoi conservarle.",
        ["Keep editing"] = "Continua a modificare",
        ["Discard edits"] = "Scarta le modifiche",
        ["The client or Vencord profile could not be checked. Review the installation and manual recovery guide."] = "Impossibile controllare il client o il profilo Vencord. Controlla l’installazione e la guida di recupero manuale.",
        ["Simulation complete. No Discord or Vencord files or processes were changed."] = "Simulazione completata. Nessun file o processo di Discord o Vencord è stato modificato.",
        ["Downloading and verifying the official Vencord installer…"] = "Download e verifica dell’installer ufficiale di Vencord…",
        ["Closing Discord after your confirmation…"] = "Chiusura di Discord dopo la tua conferma…",
        ["Installing / repairing official Vencord…"] = "Installazione / riparazione di Vencord ufficiale…",
        ["Saving a recovery snapshot and updating managed themes…"] = "Salvataggio di uno snapshot di recupero e aggiornamento dei temi gestiti…",
        ["Opening Discord and checking startup…"] = "Apertura di Discord e controllo dell’avvio…",
        ["Theme applied to server {0}. Discord startup confirmed. Changes are local to you."] = "Tema applicato al server {0}. Avvio di Discord confermato. Le modifiche riguardano solo te.",
        ["Personal theme disabled. Discord startup confirmed. Restore can undo this."] = "Tema personale disattivato. Avvio di Discord confermato. Il ripristino può annullarlo.",
        ["Previous managed setup restored. Discord startup confirmed."] = "Configurazione gestita precedente ripristinata. Avvio di Discord confermato.",
        ["Setup could not finish. Check the profile, disk space and manual recovery guide."] = "La preparazione non è terminata. Controlla profilo, spazio su disco e guida di recupero manuale.",
        [" No automatic retry was made."] = " Non è stato fatto un nuovo tentativo automatico.",
        ["Operation did not complete"] = "Operazione non completata",
        ["Invalid editor values."] = "Valori dell’editor non validi.",
        ["Editor values exceed the supported limits."] = "I valori dell’editor superano i limiti supportati.",
        ["The design or display font did not load. Choose another font or reload the preview."] = "Il design o il font dei titoli non si è caricato. Scegli un altro font o ricarica l’anteprima.",
        ["Wait for a valid preview of your latest edits before applying."] = "Attendi un’anteprima valida delle ultime modifiche prima di applicare.",
        ["Vencord now needs installation / repair. Click Apply again and review the setup option before continuing."] = "Vencord ora richiede installazione / riparazione. Fai di nuovo clic su Applica e controlla l’opzione di preparazione prima di continuare.",
        ["An operation is running. Wait for it to finish."] = "Un’operazione è in corso. Attendi che finisca.",
        ["Wait for the preview to finish loading, or reload it if it is stuck."] = "Attendi che l’anteprima finisca di caricarsi, oppure ricaricala se è bloccata.",
        ["Reload preview"] = "Ricarica l’anteprima",
        ["Selected server"] = "Server selezionato",
        ["Discord + Vencord ready. No installation needed."] = "Discord + Vencord pronti. Nessuna installazione necessaria.",
        ["Discord cannot start because Vencord's patcher.js is missing. The launcher repairs it with the official installer."] = "Discord non può avviarsi perché manca patcher.js di Vencord. Il launcher lo ripara con l’installer ufficiale.",
        ["Discord cannot start because Vencord's patcher.js is missing. Downloading the official installer…"] = "Discord non può avviarsi perché manca patcher.js di Vencord. Download dell’installer ufficiale…",
        ["Closing the broken Discord window and repairing official Vencord…"] = "Chiusura della finestra di errore di Discord e riparazione di Vencord ufficiale…",
        ["Opening Discord…"] = "Apertura di Discord…",
        ["Vencord repaired. Discord startup confirmed."] = "Vencord riparato. Avvio di Discord confermato.",
        ["Vencord repair"] = "Riparazione di Vencord",
        ["Vencord repair could not finish. Check the connection and the manual recovery guide."] = "La riparazione di Vencord non è riuscita. Controlla la connessione e la guida di recupero manuale.",
        ["The official Vencord installer did not complete. Official Discord files were restored so Discord can open. Retry the install before applying a theme."] = "L’installer ufficiale di Vencord non ha completato. I file ufficiali di Discord sono stati ripristinati, così Discord può aprirsi. Riprova l’installazione prima di applicare un tema.",
        ["Discord was not opened. Vencord's patcher.js is missing, so the official Discord backup was restored. Install Vencord again before applying a theme."] = "Discord non è stato aperto. Manca patcher.js di Vencord, quindi è stato ripristinato il backup ufficiale di Discord. Reinstalla Vencord prima di applicare un tema.",
        ["Discord found. Apply will offer official Vencord setup if needed."] = "Discord trovato. Applica proporrà la preparazione ufficiale di Vencord se serve.",
        ["Standard Discord desktop was not found for this Windows user."] = "Discord desktop standard non è stato trovato per questo utente Windows.",
        ["The launcher encountered an unexpected error. Close it and reopen your saved configuration. Check the manual recovery guide if an operation was interrupted."] = "Il launcher ha incontrato un errore imprevisto. Chiudilo e riapri la configurazione salvata. Se un’operazione si è interrotta, controlla la guida di recupero manuale.",
        ["Discord Theme Launcher"] = "Discord Theme Launcher",
        ["The launcher is already open for this Windows user."] = "Il launcher è già aperto per questo utente Windows.",
        [" · Preview (no changes)"] = " · Anteprima (nessuna modifica)",
        ["Discord Theme Launcher — {0}"] = "Discord Theme Launcher — {0}",
        ["Community themes"] = "Temi della community",
        ["Choose original presets. Applying this selection replaces the personal theme slot too."] = "Scegli i preset originali. Applicare questa selezione sostituisce anche lo slot del tema personale.",
        ["No login or server permissions required. Changes are local to this computer.\nIndependent community tool. Not affiliated with or endorsed by Discord.\nVencord is unofficial; Discord terms prohibit client mods. No account-safety guarantee."] = "Non servono accesso o permessi del server. Le modifiche restano su questo computer.\nStrumento indipendente della community. Non affiliato né approvato da Discord.\nVencord non è ufficiale; i termini di Discord vietano le modifiche del client. Nessuna garanzia sulla sicurezza dell’account.",
        ["Official Vencord"] = "Vencord ufficiale",
        ["Source & manual setup"] = "Sorgente e preparazione manuale",
        ["Backups"] = "Backup",
        ["Font license"] = "Licenza del font",
        ["Noto Sans - SIL Open Font License"] = "Noto Sans - SIL Open Font License",
        ["{0} theme"] = "Tema {0}",
        ["{0} sanitized theme preview"] = "Anteprima sanitizzata del tema {0}",
        ["Chat + channels + members"] = "Chat + canali + membri",
        ["Apply & open Discord"] = "Applica e apri Discord",
        ["Restore previous setup"] = "Ripristina la configurazione precedente",
        ["My calls are finished and drafts are saved. Discord may restart."] = "Ho terminato le chiamate e salvato le bozze. Discord può riavviarsi.",
        ["Install official Vencord if it is missing"] = "Installa Vencord ufficiale se manca",
        ["Vencord is already installed"] = "Vencord è già installato",
        ["Repair official Vencord (incomplete installation)"] = "Ripara Vencord ufficiale (installazione incompleta)",
        ["Preview mode — no network calls, files or Discord processes will be changed."] = "Modalità anteprima — nessuna chiamata di rete, file o processo di Discord verrà modificato.",
        ["Discord + Vencord detected. Choose themes and apply. Unchecked managed themes will be removed; other themes stay."] = "Discord + Vencord rilevati. Scegli i temi e applica. I temi gestiti non selezionati verranno rimossi; gli altri restano.",
        ["Vencord's loader is present but runtime files are missing or unreadable. Choose Repair above."] = "Il loader di Vencord c’è, ma i file di runtime mancano o non si leggono. Scegli Ripara qui sopra.",
        ["Discord detected. Enable Vencord installation above for first setup."] = "Discord rilevato. Attiva l’installazione di Vencord qui sopra per la prima preparazione.",
        ["Run normally, without Administrator privileges."] = "Eseguilo normalmente, senza privilegi di amministratore.",
        ["The profile could not be read. Check the manual setup guide before applying."] = "Il profilo non si è letto. Controlla la guida di preparazione manuale prima di applicare.",
        ["Finish any call or stream and save your drafts. Then tick the restart checkbox."] = "Termina chiamate o stream e salva le bozze. Poi seleziona la casella di riavvio.",
        ["Before restarting Discord"] = "Prima di riavviare Discord",
        ["Vencord is incomplete. Choose Repair official Vencord above, or repair it with the official installer first."] = "Vencord è incompleto. Scegli Ripara Vencord ufficiale qui sopra, oppure riparalo prima con l’installer ufficiale.",
        ["Choose official Vencord installation above, or install it manually first."] = "Scegli l’installazione ufficiale di Vencord qui sopra, oppure installalo prima manualmente.",
        ["Downloading and verifying {0}…"] = "Download e verifica di {0}…",
        ["A bundled theme does not match this launcher version. Download a fresh portable package."] = "Un tema incluso non corrisponde a questa versione del launcher. Scarica un pacchetto portatile nuovo.",
        ["Downloading the official Vencord installer…"] = "Download dell’installer ufficiale di Vencord…",
        ["Closing Discord…"] = "Chiusura di Discord…",
        ["Repairing official Vencord…"] = "Riparazione di Vencord ufficiale…",
        ["Installing official Vencord…"] = "Installazione di Vencord ufficiale…",
        ["Restoring previous setup…"] = "Ripristino della configurazione precedente…",
        ["Backing up and applying your selection…"] = "Backup e applicazione della selezione…",
        ["Previous managed themes restored. Discord startup confirmed."] = "Temi gestiti precedenti ripristinati. Avvio di Discord confermato.",
        ["Done. Discord startup confirmed. Each selected theme appears in its matching server."] = "Fatto. Avvio di Discord confermato. Ogni tema selezionato compare nel server corrispondente.",
        ["The download could not finish. Check your connection and the release page, then retry."] = "Il download non è terminato. Controlla la connessione e la pagina della release, poi riprova.",
        ["Setup could not finish. Check available disk space, the profile and the manual recovery guide."] = "La preparazione non è terminata. Controlla spazio su disco, profilo e guida di recupero manuale.",
        ["Setup did not complete"] = "Preparazione non completata",
        ["No backup folder exists yet. It is created before the first change."] = "La cartella dei backup non esiste ancora. Viene creata prima della prima modifica.",
        ["Could not open the link or folder. Use the repository URL in the release notes."] = "Impossibile aprire il link o la cartella. Usa l’URL del repository nelle note di release.",
        ["Import a static font licensed for embedding and sharing"] = "Importa un font statico con licenza di incorporamento e condivisione",
        ["Static fonts (*.ttf;*.otf)"] = "Font statici (*.ttf;*.otf)",
        ["Select this font's redistribution license (TXT)"] = "Seleziona la licenza di ridistribuzione di questo font (TXT)",
        ["License text (*.txt)"] = "Testo della licenza (*.txt)",
        ["I have permission to embed and redistribute this font\nand its license with shared theme configurations."] = "Ho il permesso di incorporare e ridistribuire questo font\ne la sua licenza con le configurazioni del tema condivise.",
        ["Import font"] = "Importa font",
        ["Close the launcher and run it normally, without Administrator privileges."] = "Chiudi il launcher ed eseguilo normalmente, senza privilegi di amministratore.",
        ["Install the standard Discord desktop client for this Windows user first. PTB, Canary, Store and custom installations require manual setup."] = "Installa prima il client Discord desktop standard per questo utente Windows. PTB, Canary, Store e le installazioni personalizzate richiedono la preparazione manuale.",
        ["A custom Vencord data directory is configured. Use manual theme installation for this profile."] = "È configurata una cartella dati di Vencord personalizzata. Per questo profilo installa il tema manualmente.",
        ["Discord's app folder was not found."] = "La cartella dell’app di Discord non è stata trovata.",
        ["An unrecognized client modification was detected. Use the manual installation route to avoid replacing it."] = "È stata rilevata una modifica del client non riconosciuta. Usa l’installazione manuale per non sostituirla.",
        ["Could not verify a running Discord process. Fully quit Discord, including the tray, and try again."] = "Impossibile verificare un processo di Discord in esecuzione. Chiudi Discord del tutto, anche dall’area di notifica, e riprova.",
        ["The official Vencord installer could not start."] = "L’installer ufficiale di Vencord non si è avviato.",
        ["The official installer timed out. Retry or use the official manual installer."] = "L’installer ufficiale ha superato il tempo previsto. Riprova o usa l’installer manuale ufficiale.",
        ["The official Vencord installer did not complete. Use the official manual installer, then reopen this launcher."] = "L’installer ufficiale di Vencord non è terminato. Usa l’installer manuale ufficiale, poi riapri questo launcher.",
        ["Vencord is incomplete. Missing or unreadable: {0}. Choose Repair official Vencord before reopening Discord."] = "Vencord è incompleto. Mancanti o illeggibili: {0}. Scegli Ripara Vencord ufficiale prima di riaprire Discord.",
        ["Discord's updater could not start."] = "L’aggiornatore di Discord non si è avviato.",
        ["Theme files were saved, but Discord opened an error window. Setup is not confirmed. Use the official Vencord Repair option and check Discord before retrying."] = "I file del tema sono stati salvati, ma Discord ha aperto una finestra di errore. La preparazione non è confermata. Usa la riparazione ufficiale di Vencord e controlla Discord prima di riprovare.",
        ["Theme files were saved, but Discord startup could not be confirmed. Check its window; if Vencord reports an error, use the official Repair option."] = "I file del tema sono stati salvati, ma l’avvio di Discord non è stato confermato. Controlla la finestra; se Vencord segnala un errore, usa la riparazione ufficiale.",
        ["Paste a server ID (17–20 digits) or an https://discord.com/channels/server/channel link. Invite links do not contain a server ID."] = "Incolla un ID server (17–20 cifre) o un link https://discord.com/channels/server/channel. I link di invito non contengono un ID server.",
        ["This configuration version is not supported."] = "Questa versione della configurazione non è supportata.",
        ["Use a theme name of 1–80 characters."] = "Usa un nome del tema di 1–80 caratteri.",
        ["Save the target as a server ID."] = "Salva la destinazione come ID server.",
        ["Unknown preset, palette, font or artwork choice."] = "Preset, tavolozza, font o grafica sconosciuti.",
        ["Colors must use #RRGGBB."] = "I colori devono usare #RRGGBB.",
        ["Choose a darker surface to keep messages and the composer readable."] = "Scegli una superficie più scura per tenere leggibili messaggi e casella di scrittura.",
        ["Import the selected custom asset first."] = "Importa prima l’asset personalizzato selezionato.",
        ["Invalid asset metadata or asset size exceeds its limit."] = "Metadati dell’asset non validi, oppure la dimensione supera il limite.",
        ["Custom fonts require TTF/OTF bytes and their redistribution license text."] = "I font personalizzati richiedono i byte TTF/OTF e il testo della licenza di ridistribuzione.",
        ["Shared images must be normalized PNG files. Import a PNG or JPEG in the launcher."] = "Le immagini condivise devono essere PNG normalizzati. Importa un PNG o un JPEG nel launcher.",
        ["Images must be at most 4096 × 4096 and 16 million pixels."] = "Le immagini devono essere al massimo 4096 × 4096 e 16 milioni di pixel.",
        ["The font signature does not match its format. Collections, WOFF and variable fonts are not imported in this version."] = "La firma del font non corrisponde al formato. Raccolte, WOFF e font variabili non si importano in questa versione.",
        ["Invalid font table directory."] = "Directory delle tabelle del font non valida.",
        ["Invalid font table bounds."] = "Limiti delle tabelle del font non validi.",
        ["Use a static TTF/OTF font; variable font import is not supported."] = "Usa un font TTF/OTF statico; l’importazione di font variabili non è supportata.",
        ["Invalid font embedding flags."] = "Flag di incorporamento del font non validi.",
        ["This font restricts embedding. Choose a font licensed for editable embedding and redistribution."] = "Questo font limita l’incorporamento. Scegline uno con licenza di incorporamento modificabile e ridistribuzione.",
        ["The font is missing required tables."] = "Al font mancano tabelle obbligatorie.",
        ["Configuration exceeds 24 MiB."] = "La configurazione supera 24 MiB.",
        ["Invalid configuration."] = "Configurazione non valida.",
        ["Configuration exceeds 24 MiB. Use smaller images."] = "La configurazione supera 24 MiB. Usa immagini più piccole.",
        ["Images must be at most 8 MiB."] = "Le immagini devono essere al massimo 8 MiB.",
        ["Choose a PNG or JPEG image."] = "Scegli un’immagine PNG o JPEG.",
        ["The file is not a PNG or JPEG image."] = "Il file non è un’immagine PNG o JPEG.",
        ["Fonts must be at most 2 MiB."] = "I font devono essere al massimo 2 MiB.",
        ["Choose a static TTF or OTF font."] = "Scegli un font TTF o OTF statico.",
        ["The font license must be at most 64,000 bytes."] = "La licenza del font deve essere al massimo 64.000 byte.",
        ["Vencord settings are not a JSON object. Restore or repair them before continuing."] = "Le impostazioni di Vencord non sono un oggetto JSON. Ripristinale o riparale prima di continuare.",
        ["Vencord enabledThemes has an unsupported format."] = "enabledThemes di Vencord ha un formato non supportato.",
        ["The download failed its integrity check. Nothing was installed."] = "Il download non ha superato il controllo di integrità. Non è stato installato nulla.",
        ["Unsupported download source."] = "Origine del download non supportata.",
        ["Download exceeds the size limit."] = "Il download supera il limite di dimensione.",
        ["Unknown theme selection."] = "Selezione del tema sconosciuta.",
        ["A selected download is missing."] = "Manca un download selezionato.",
        ["Invalid backup reference."] = "Riferimento del backup non valido.",
        ["Invalid backup."] = "Backup non valido.",
        ["The backup does not match this launcher."] = "Il backup non corrisponde a questo launcher.",
        ["A backup file is missing."] = "Manca un file del backup."
    };

    static UiText()
    {
        LoadImportText();
        foreach (var key in Italian.Keys)
            if (!Spanish.ContainsKey(key) || !Russian.ContainsKey(key) || !German.ContainsKey(key) || Spanish[key].Length == 0 || Russian[key].Length == 0 || German[key].Length == 0)
                throw new InvalidOperationException("Missing interface translation: " + key);
    }
}
