using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml;

namespace EventViewer.Controllers
{
    public class ServerSideEvent
    {
        public string DateAndTime { get; set; }
        public string Source { get; set; }
        public string EventID { get; set; }
        public string TaskCategory { get; set; }
        public string Description { get; set; }
        public string Level { get; set; }

        public string EventRecordID { get; set; }
        public string Computer { get; set; }

        public ServerSideEvent()
        { }

    }

    public static class Utils
    {
        public const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const int FORMAT_MESSAGE_FROM_STRING = 0x00000400;
        public const int FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        public const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        public const int FORMAT_MESSAGE_MAX_WIDTH_MASK = 0x000000FF;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const string Kernel32 = "kernel32.dll";
        public const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;



        [System.Flags]
        public enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        [DllImport(Kernel32, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [DllImport(Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = true)]
        public static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId,
            int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr[] arguments);

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        static public IntPtr GetMessageResources(string fullDLLNameWithPath)
        {
            IntPtr ModToLoad = IntPtr.Zero;
            ModToLoad = Utils.LoadLibraryEx(fullDLLNameWithPath, IntPtr.Zero, Utils.LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
            return ModToLoad;
        }

        private static bool ExtractDateTimeFromFormattedEvent(string formatEventMessage, out DateTime parsedDateTime)
        {
            string[] arrformatEventMessage = formatEventMessage.Split(Environment.NewLine.ToCharArray());

            string strDateTime = "";
            foreach (var line in arrformatEventMessage)
            {
                if (line.StartsWith("Event time"))
                {
                    strDateTime = line.Substring(12);
                    break;
                }
            }

            if (strDateTime != "")
            {
                if (DateTime.TryParse(strDateTime, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None, out parsedDateTime))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                parsedDateTime = DateTime.MinValue;
                return false;
            }

        }


        public static string UnsafeTryFormatMessage(IntPtr hModule, uint messageNum, string[] insertionStrings)
        {
            string msg = null;

            int msgLen = 0;
            StringBuilder buf = new StringBuilder(1024);
            int flags = Utils.FORMAT_MESSAGE_FROM_HMODULE | Utils.FORMAT_MESSAGE_ARGUMENT_ARRAY; //| Utils.FORMAT_MESSAGE_FROM_SYSTEM;

            IntPtr[] addresses = new IntPtr[insertionStrings.Length];
            GCHandle[] handles = new GCHandle[insertionStrings.Length];
            GCHandle stringsRoot = GCHandle.Alloc(addresses, GCHandleType.Pinned);

            // Make sure that we don't try to pass in a zero length array of addresses.  If there are no insertion strings, 
            // we'll use the FORMAT_MESSAGE_IGNORE_INSERTS flag . 
            // If you change this behavior, make sure you look at TryFormatMessage which depends on this behavior!
            if (insertionStrings.Length == 0)
            {
                flags |= Utils.FORMAT_MESSAGE_IGNORE_INSERTS;
            }

            try
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = GCHandle.Alloc(insertionStrings[i], GCHandleType.Pinned);
                    addresses[i] = handles[i].AddrOfPinnedObject();
                }
                int lastError = Utils.ERROR_INSUFFICIENT_BUFFER;
                while (msgLen == 0 && lastError == Utils.ERROR_INSUFFICIENT_BUFFER)
                {
                    msgLen = Utils.FormatMessage(
                        flags,
                        hModule,
                        messageNum,
                        0,
                        buf,
                        buf.Capacity,
                        addresses);

                    if (msgLen == 0)
                    {
                        lastError = Marshal.GetLastWin32Error();
                        if (lastError == Utils.ERROR_INSUFFICIENT_BUFFER)
                            buf.Capacity = buf.Capacity * 2;
                        else
                        {
                            msg = string.Format("FormatMessage Failed with error = {0} for messageNum = {1} for hmodule {2} ", lastError.ToString(), messageNum, hModule.ToString());
                        }
                    }
                }
            }
            catch
            {
                msgLen = 0;              // return empty on failure
            }
            finally
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].IsAllocated) handles[i].Free();
                }
                stringsRoot.Free();
            }

            if (msgLen > 0)
            {
                msg = buf.ToString();
                // chop off a single CR/LF pair from the end if there is one. FormatMessage always appends one extra.
                if (msg.Length > 1 && msg[msg.Length - 1] == '\n')
                    msg = msg.Substring(0, msg.Length - 2);
            }

            return msg;
        }

        public static long GenerateHexEventIdFromDecimalEventId(int MessageId, string Severity)
        {
            // From http://referencesource.microsoft.com/#System.ServiceModel.Internals/System/Runtime/Diagnostics/EventLogEventId.cs
            // When adding an EventLogEventId, an entry must also be added to src\ndp\cdf\src\WCF\EventLog\EventLog.mc.
            // The hexadecimal representation of each EventId ('0xabbbcccc') can be broken down into 3 parts:
            //     Hex digit  1   ('a')    : Severity : a=0 for Success, a=4 for Informational, a=8 for Warning, a=c for Error
            //     Hex digits 2-4 ('bbb')  : Facility : bbb=001 for Tracing, bbb=002 for ServiceModel, bbb=003 for TransactionBridge, bbb=004 for SMSvcHost, bbb=005 for Info_Cards, bbb=006 for Security_Audit
            //     Hex digits 5-8 ('cccc') : Code     : Each event within the same facility is assigned a unique "code".

            string sevInHexString = (Convert.ToInt32(Severity) * 4).ToString("X");

            string strHex = sevInHexString + "000";

            string strMessageIdHex = MessageId.ToString("X");

            //Add training "0" till the length is 4 digits 
            while (strMessageIdHex.Length < 4)
                strMessageIdHex = "0" + strMessageIdHex;

            strHex = strHex + strMessageIdHex;

            long returnValue = 0;
            if (long.TryParse(strHex, System.Globalization.NumberStyles.HexNumber, null, out returnValue))
            {
            }
            else
            {
                //TODO Thinking about handling the error here
            }
            return returnValue;


        }

        public static bool IsRunningInMAWS()
        {
            if (HttpContext.Current.Request.Headers["Host"].Contains("scm") || HttpContext.Current.Request.Headers["Host"].Contains("azurewebsites.net"))
                return true;
            else
                return false;
        }

        public static IEnumerable<ServerSideEvent> GetEvents()
        {
            string eventLogXmlFile = "";
            string aspnet_rcFile = "";
            string pwrshmsgFile = "";

            if (Utils.IsRunningInMAWS())
            {
                eventLogXmlFile = Environment.ExpandEnvironmentVariables(@"%HOME%\LogFiles\eventlog.xml");
            }
            else
            {
                eventLogXmlFile = HttpContext.Current.Server.MapPath("~/App_Data/eventlog.xml");
            }

            aspnet_rcFile = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\aspnet_rc.dll");
            pwrshmsgFile = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\WindowsPowerShell\v1.0\pwrshmsg.dll");

            IntPtr g_hResourcesASPNET = IntPtr.Zero;
            IntPtr g_hResourcesPowerShell = IntPtr.Zero;


            // As per http://msdn.microsoft.com/en-us/library/windows/desktop/ms684179(v=vs.85).aspx
            // If LoadLibraryEx is called twice for the same file with LOAD_LIBRARY_AS_DATAFILE, LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE, 
            // or LOAD_LIBRARY_AS_IMAGE_RESOURCE, two separate mappings are created for the file
            // I realize that if I don't cache the handle the MappedFile portion under !address -summary increases
            // Not really sure on the after effects of storing the handle pointer somewhere.

            if (HttpContext.Current.Cache["g_hResourcesASPNET"] != null)
            {
                g_hResourcesASPNET = (IntPtr)HttpContext.Current.Cache["g_hResourcesASPNET"];
            }
            else
            {
                g_hResourcesASPNET = Utils.GetMessageResources(aspnet_rcFile);
                HttpContext.Current.Cache["g_hResourcesASPNET"] = g_hResourcesASPNET;
            }


            if (HttpContext.Current.Cache["g_hResourcesPowerShell"] != null)
            {
                g_hResourcesPowerShell = (IntPtr)HttpContext.Current.Cache["g_hResourcesPowerShell"];
            }
            else
            {
                g_hResourcesPowerShell = Utils.GetMessageResources(pwrshmsgFile);
                HttpContext.Current.Cache["g_hResourcesPowerShell"] = g_hResourcesPowerShell;
            }

            g_hResourcesASPNET = Utils.GetMessageResources(aspnet_rcFile);
            g_hResourcesPowerShell = Utils.GetMessageResources(pwrshmsgFile);

            if (!File.Exists(eventLogXmlFile))
            {
                yield break;
            }

            System.Xml.XmlDocument dom = new System.Xml.XmlDocument();
            

            dom.Load(eventLogXmlFile);

            System.Xml.XmlNodeList xmlList = dom.SelectNodes("/Events/Event");

            for (int i = (xmlList.Count - 1); i >= 0; i--)
            {
                XmlNode EventNode = xmlList[i];
                var node = EventNode.SelectSingleNode("System");

                var EventDataNode = EventNode.SelectSingleNode("EventData");

                var evt = new ServerSideEvent();

                string strProvider = node["Provider"].GetAttribute("Name");
                evt.Source = strProvider;

                string dateTimeString = node["TimeCreated"].GetAttribute("SystemTime");

                bool booValidDateFound = false;

                if (dateTimeString.Contains("T") && dateTimeString.Contains("Z"))
                {
                    //So we have the full date and time here...Parse it using TryParse.

                    DateTime resultDateTime;
                    if (DateTime.TryParse(dateTimeString, out resultDateTime))
                    {
                        evt.DateAndTime = resultDateTime.ToString();
                        booValidDateFound = true;
                    }
                    else
                    {
                        booValidDateFound = false;
                        evt.DateAndTime = node["TimeCreated"].GetAttribute("SystemTime");
                    }

                }
                else
                {
                    evt.DateAndTime = node["TimeCreated"].GetAttribute("SystemTime");
                }
                evt.EventID = node["EventID"].InnerText;
                evt.TaskCategory = node["Task"].InnerText;
                evt.EventRecordID = node["EventRecordID"].InnerText;
                evt.Computer = node["Computer"].InnerText;

                List<string> arrayOfdata = new List<string>();

                foreach (XmlNode datanode in EventDataNode.ChildNodes)
                {
                    arrayOfdata.Add(datanode.InnerText);
                }

                string[] args = arrayOfdata.ToArray();
                int MessageId = Convert.ToInt32(node["EventID"].InnerText);

                string strLevel = node["Level"].InnerText;

                if (strProvider.StartsWith("ASP.NET"))
                {
                    string formatEventMessage = "";
                    long longHexEventId = Utils.GenerateHexEventIdFromDecimalEventId(MessageId, strLevel);
                    formatEventMessage = Utils.UnsafeTryFormatMessage(g_hResourcesASPNET, Convert.ToUInt32(longHexEventId), args);

                    if (!booValidDateFound)
                    {
                        DateTime parsedDateTime;
                        if (ExtractDateTimeFromFormattedEvent(formatEventMessage, out parsedDateTime))
                        {
                            evt.DateAndTime = parsedDateTime.ToString();
                        }
                        else
                        {
                            evt.DateAndTime = node["TimeCreated"].GetAttribute("SystemTime");
                        }
                    }

                    if (formatEventMessage.StartsWith("FormatMessage Failed with error"))
                    {
                        formatEventMessage = "<b>" + formatEventMessage + "</b>\n DLL = " + aspnet_rcFile + " \n Showing Raw Event\n\n" + EventDataNode.InnerXml;
                    }

                    evt.Description = formatEventMessage;
                }
                else if (strProvider.StartsWith("PowerShell"))
                {
                    string formatEventMessage = "";
                    long longHexEventId = Utils.GenerateHexEventIdFromDecimalEventId(MessageId, strLevel);
                    formatEventMessage = Utils.UnsafeTryFormatMessage(g_hResourcesPowerShell, Convert.ToUInt32(longHexEventId), args);

                    if (formatEventMessage.StartsWith("FormatMessage Failed with error"))
                    {
                        formatEventMessage = "<b>" + formatEventMessage + "</b>\n DLL = " + pwrshmsgFile + " \n Showing Raw Event\n\n" + EventDataNode.InnerXml;
                    }

                    evt.Description = formatEventMessage;
                }
                else
                {
                    evt.Description = string.Join(Environment.NewLine, args);
                }
                evt.Level = node["Level"].InnerText;

                yield return evt;
            }
            
        }
    }

    public class EventsController : ApiController
    {
        // GET: api/Events

        public IEnumerable<ServerSideEvent> Get()
        {
            return Utils.GetEvents();
        }

    }
}
