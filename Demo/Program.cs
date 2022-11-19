using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XtraLibrary;
using XtraLibrary.SecsGem;

namespace XtraLibraryDemo
{
    internal class Program
    {
        static Task task;
        static HsmsHost host;
        static CancellationTokenSource cancelSource;
        static void Main(string[] args)
        {
            SecsHostFactory factory = new SecsHostFactory();
            EquipmentModel equipment = new EquipmentModel("test");
            equipment.Connection.DeviceId = 10;
            equipment.Connection.Protocol = GemProtocol.HSMS;
            HsmsParameters parameters = new HsmsParameters()
            {
                IPAddress = "127.0.0.1",
                PortNo = 5000,
                Mode = HsmsConnectProcedure.ACTIVE
            };
            equipment.Connection.HsmsParameters = parameters;
            host = factory.Create(equipment) as HsmsHost;
            host.ReceivedPrimaryMessage += ReceivedPrimaryMessage;
            host.ReceivedSecondaryMessage += ReceivedSecondaryMessage;
            host.ErrorNotification += ErrorNotification;
            host.ConversionErrored += ConversionErrored;
            host.TracedSmlLog += TracedSmlLog;
            host.HsmsStateChanged += HsmsStateChanged;
            //
            try
            {
                host.Connect();
                cancelSource = new CancellationTokenSource();
                task = Task.Run(HostAction);
                Console.WriteLine("Press any key to stop!");
                Console.ReadKey();
                cancelSource.Cancel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            //
            task.Wait();
            host.Disconnect();
            Console.WriteLine("Device is disconnected!");
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }

        private static void HsmsStateChanged(object sender, HsmsStateChangedEventArgs e)
        {
            Console.WriteLine("############################");
            Console.WriteLine("#  Hsms State Changed");
            Console.WriteLine("# " + e.State);
            Console.WriteLine("############################");
            Console.WriteLine();
        }

        private static void ErrorNotification(object sender, SecsErrorNotificationEventArgs e)
        {
            Console.WriteLine("############################");
            Console.WriteLine("#  Error Notification");
            Console.WriteLine("# " + e.Source);
            Console.WriteLine("# " + e.Message);
            Console.WriteLine("# " + e.TransactionId.ToString());
            Console.WriteLine("############################");
            Console.WriteLine();
        }

        private static void ConversionErrored(object sender, ConversionErrorEventArgs e)
        {
            
            Console.WriteLine("############################");
            Console.WriteLine("#  Conversion Error");
            Console.WriteLine("# " + e.Exception.ToString());
            Console.WriteLine("############################");
            Console.WriteLine();
        }

        private static void TracedSmlLog(object sender, TraceLogEventArgs e)
        {
            //return;
            Console.WriteLine("############################");
            Console.WriteLine("#  SML TRACE");
            Console.WriteLine("# " + e.TimeStamp);
            Console.WriteLine("# " + e.Direction);
            Console.WriteLine("# " + e.LogMessage);
            Console.WriteLine("# " + e.SML);
            Console.WriteLine("############################");
            Console.WriteLine();
        }

        private static void ReceivedPrimaryMessage(object sender, PrimarySecsMessageEventArgs e)
        {
            Console.WriteLine("Recv Primary: " + e.Primary.ToString());
            if (e.Primary.NeedReply)
            {
                if ((e.Primary.Stream == 1) && (e.Primary.Function == 1))
                {
                    var answer = new SecsMessage(1, 2, false);
                    host.Reply(e.Primary, answer);
                    Console.WriteLine($"Sent Answer: S{answer.Stream}F{answer.Function}");
                }
                else
                if ((e.Primary.Stream == 1) && (e.Primary.Function == 13))
                {
                    var answer = new SecsMessage(1, 14, false);
                    answer.Items.Add(new SecsItemBinary("B", new byte[] { 0x00 }));
                    var list = new SecsItemList("L");
                    list.AddItem(new SecsItemAscii("A1", "0"));
                    list.AddItem(new SecsItemAscii("A1", "0"));
                    answer.Items.Add(list);
                    host.Reply(e.Primary, answer);
                    Console.WriteLine($"Sent Answer: S{answer.Stream}F{answer.Function}");
                }
                else
                {
                    host.Reply_AbortMessage(e.Primary);
                    Console.WriteLine("Sent Answer: Abort");
                }
            }
        }

        private static void ReceivedSecondaryMessage(object sender, SecondarySecsMessageEventArgs e)
        {
            var s = SmlBuilder.ToSmlString(e.Secondary);
            Console.WriteLine("Secondary: " + s);// e.Secondary.ToString());
        }

        static void HostAction()
        {
            while (true)
            {
                if (cancelSource.Token.IsCancellationRequested)
                    return;
                if (host.State == HsmsState.SELECTED)
                {
                    var msg = new SecsMessage(1, 1, true);
                    host.Send(msg);
                    Console.WriteLine("Sent: S1F1");
                }
                else
                    Console.WriteLine("Device is offline!");
                //
                if (cancelSource.Token.IsCancellationRequested)
                    return;
                Task.Delay(100).Wait();
            }
        }
    }
}
