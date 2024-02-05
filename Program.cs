using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI.WPF;

namespace HelloWorldWpfSetup
{
    internal static class Program
    {
        static void Main()
        {
            var project = new ManagedProject(
                "Hello World APP",
                new Dir(@"%ProgramFiles%\HelloWorldApp", new WixSharp.File("readme.txt")));

            project.GUID = new Guid("{8E51CFD0-DEC3-4EA2-8F3F-D9457D43BEA9}");

            project.Version = new Version("1.0.0.0");
            project.InstallScope = InstallScope.perUser;

            // project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs
            project.Actions = new[]
            {
                new ManagedAction(
                    CustomActions.CheckProcesses,
                    Return.check,
                    When.Before,
                    Step.InstallInitialize,
                    WixSharp.Condition.NOT_Installed)
            };

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<HelloWorldWpfSetup.WelcomeDialog>()
                                            //.Add<HelloWorldWpfSetup.LicenceDialog>()
                                            //.Add<HelloWorldWpfSetup.FeaturesDialog>()
                                            //.Add<HelloWorldWpfSetup.InstallDirDialog>()
                                            .Add<HelloWorldWpfSetup.ProgressDialog>()
                                            .Add<HelloWorldWpfSetup.ExitDialog>();

            //project.ManagedUI.ModifyDialogs.Add<HelloWorldWpfSetup.MaintenanceTypeDialog>()
            //                               .Add<HelloWorldWpfSetup.FeaturesDialog>()
            //                               .Add<HelloWorldWpfSetup.ProgressDialog>()
            //                               .Add<HelloWorldWpfSetup.ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";
            // based on this sample: wixsharp\Source\src\WixSharp.Samples\Wix# Samples\Managed Setup\MultiLanguageUI
            project.Localize();

            project.BuildMsi();
        }

        static void Localize(this ManagedProject project)
        {
            project
                .AddBinary(new Binary(new Id("en_xsl"), "Resources\\WixUI_en-US.wxl"))
                .AddBinary(new Binary(new Id("es_xsl"), "Resources\\WixUI_es-ES.wxl"));

            project.UIInitialized += (SetupEventArgs e) =>
            {
                MsiRuntime runtime = e.ManagedUI.Shell.MsiRuntime();

                CultureInfo culture = CultureInfo.InstalledUICulture;
                //var osLanguage = culture.TwoLetterISOLanguageName.ToLower();
                var osLanguage = "es";
                switch (osLanguage)
                {
                    case "es":
                        runtime.UIText.InitFromWxl(e.Session.ReadBinary("es_xsl"), merge: true);
                        break;
                    default:
                        runtime.UIText.InitFromWxl(e.Session.ReadBinary("en_xsl"), merge: true);
                        break;
                }
            };
        }
    }

    /// <summary>
    /// Seems the UICustom translation is not recognized.
    /// And the standard one is not translated to "es".
    /// </summary>
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CheckProcesses(Session session)
        {
            if (IsProcessRunning("notepad++"))
            {
                var runtime = new MsiRuntime(session);

                var msg = "Custom msg: [UICustomNotePadPPRunning] translated!";
                var rawMsg = "Std msg: [WelcomeDlgTitle] translated!";

                var translatedMsg = msg.LocalizeWith(runtime.Localize);
                var translatedRawMsg = rawMsg.LocalizeWith(runtime.Localize);

                MessageBox.Show(translatedMsg);
                MessageBox.Show(translatedRawMsg);


                session.Log("notepad++ is running. Please close it before running the setup.");

                session.Message(
                    InstallMessage.Error,
                    new Record(translatedMsg));

                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        // Helper class to wrap IntPtr as IWin32Window
        public class WindowWrapper : IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }

}