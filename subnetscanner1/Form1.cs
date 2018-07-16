using System;
using System.Windows.Forms;
using System.Threading;
using System.Net; //bibliotek til at tilgå netværket, indeholder en masse classes som skal bruges bla. IPHostentry og dns info
using System.Net.NetworkInformation; // bibliotek til at tilgå classes der kan bruges til ping.
using System.Net.Sockets; //bliver i den her her program brugt til hvis der kommer svar fra en host uden svar uden navn (firewall)



namespace subnetscanner1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent(); 

            //Thread Control der "desværre" er brug for. siger at der ikke skal tjekkes for crossthreads
            Control.CheckForIllegalCrossThreadCalls = false;
        }


        //counter til antal fundet host
        static CountdownEvent countdown; //event til total antl host.
        static int upCount = 0; // tæller til Host. starter på 0
        static object Obj = new object(); //laver et object til tæller af host (hver gang der bliver fundet en i vores loop)
        const bool resolveNames = true; // vi laver en konstant variabel som en Bool til at gemme alle host navne. en Bool kan kun ha 2 værdier (sandt eller falsk)

        //Thread der søger på det først starter når der bliver trykket på scan
        Thread myThread = null;

        //Ping completed hvad der sker når der kommer et svar tilbage fra et ping dette bliver gjort for hvert enkelt tal fra 1-255
        private void P_PingCompleted(object sender, PingCompletedEventArgs e) //vi laver en metode/funktion pingcompletedeventargs (e) håndtere data fra hvert ping der kommer tilbage.
        {
            string ip = (string)e.UserState; //laver en string til IP'en. UserState bliver brugt til asynkron modtagelse af svar fra MyPing.
            if (e.Reply != null && e.Reply.Status == IPStatus.Success) //hvis ping kommer tilbage med en et svar og svaret er en succes
            {
                if (resolveNames) //hvis der er reply fra en host finder den navn og IP på den host
                {
                    string name; //laver en variable "name" til at gemme data fra den host.
                    try
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(ip); //tilføjer IP'en fra variablen ip. (reply som string)
                        name = hostEntry.HostName; //gemmer computernavnet + netværksnavn i variablen name
                    }
                    catch (SocketException) //hvis der er svar, men den ikke kan få navn på host
                    {
                        name = " Skjult af Firewall"; //for host name "skjult af firewall"
                    }
                    txtHosts.AppendText(ip + " " + name + "\r\n"); //skriver ud til vinduet 
                }
                lock (Obj)
                {
                    upCount++; //giver et signal til countdown om at der er fundet en host
                }
            }
            countdown.Signal(); // snakker sammen med den anden countdown.signal om hvor mange der er fundet
        }

        //Loop der pinger efter andre enheder i subnet
        public void scan(string subnet)
        {
            countdown = new CountdownEvent(1); //tæller hvor mange host der bliver fundet
            string ipBase = txtIP.Text; //variable ipBase som bliver brugt til input til scanner
            for (int i = 1; i < 255; i++) // for loop som scanner fra 1 til 255
            {
                string ip = ipBase + i.ToString(); //variable ip som tager input og tilføjere 1-255 fra loopet

                Ping myPing = new Ping(); // vi pinger hvert tal fra 1-255
                myPing.PingCompleted += new PingCompletedEventHandler(P_PingCompleted); //kører igennem funktionen i bunden.
                countdown.AddCount(); //tilføjer 1 til tæller
                myPing.SendAsync(ip, 10000, ip); //sender asynkront ud til de 255 ip'er der er. venter i op til 10000ms på svar. 
            }
            countdown.Signal(); // snakker sammen med den anden countdown.signal om hvor mange der er fundet
            countdown.Wait(); //venter med at sende at give det endelige resultat til upCount til der er svar fra alle signaler i loopet.

            txtHosts.AppendText(upCount + " Fundet total"); //skriver hvor mange host der er fundet.
        }
        //Scan knappen
        private void cmdScan_Click(object sender, EventArgs e) //start knap til subnet scanner
        {           
            myThread = new Thread(() => scan(txtIP.Text));
            myThread.Start();

            IPHostEntry host;
            string localIP;

            host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetworkV6")
                {
                    localIP = ip.ToString();
                }
            }

            if (myThread.IsAlive) //når der bliver trykket scan
            {
                cmdReset.Enabled = true; //reset knappen er synlig
                cmdScan.Enabled = false; //man kan ikke trykke på scan
                txtIP.Enabled = false; //ip add. vinduet er kan man ikke skrive i
            }
            else //sætte dem tilbage så det er muligt at skrive en ip og trykke på scan
            {              
                cmdScan.Enabled = true; 
                txtIP.Enabled = true;
            }
        }

        private void cmdReset_Click(object sender, EventArgs e) // clear knap til subnet scanner, som nulstiller  (reset knappen)
        {
            cmdScan.Enabled = true;
            cmdReset.Enabled = false;
            txtIP.Enabled = true;
            upCount = 0;
            txtHosts.Clear();
            txtIP.Clear();
            txtIP.Focus();
        }
    }
}
