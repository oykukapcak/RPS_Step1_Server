using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

struct ikili{
    public int invited;
    public int inviter;
    public String invited_choice;
    public String inviter_choice;
    public int roundNo;
    public int invited_score;
    public int inviter_score;
}

namespace RPS_Step1_Server
{
    public partial class Form1 : Form
    {
        bool listening = false;
        bool terminating = false;
        bool accept = true;
        Socket server;
        List<Socket> socketList;
        List<String> userList;
        List<String> playingAtm;
        List<String> prePlaying;
        List<ikili> playList;
        List<int> scoreBoard;
        int serverPort;
        Thread thrAccept;
        int invUser;
        int gameNo;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketList = new List<Socket>();
            userList = new List<String>();
            playingAtm = new List<String>();
            prePlaying = new List<String>();
            playList = new List<ikili>();
            scoreBoard = new List<int>();
        }
        
        //function is used in a thread so that new clients will be able to connect even if server is busy
        private void Accept()
        {
            while (accept)
            {
                try
                {
                    socketList.Add(server.Accept()); //adds the socket used for connecting to client to socketList
                    richTextBox1.Text+=("New client connected...\n");

                    //starts a thread with receive function
                    Thread thrReceive;
                    thrReceive = new Thread(new ThreadStart(Receive));
                    thrReceive.IsBackground = true;
                    thrReceive.Start();
                }
                catch
                {
                    if (terminating)
                        accept = false;
                    else
                        richTextBox1.Text+=("Listening socket has stopped working...\n");
                }
            }
        }

        //this function is used in thread and displays the received message in the console 
        private void Receive()
        {
            bool connected = true;
            bool nameEntered = false;
            string clientName = "";
            Socket n = socketList[socketList.Count - 1];

            int curUser = socketList.Count - 1;
            string invited;
            

            while (connected)  //while the connection has not ended
            {
                try
                {
                    //checks for incoming buffers
                    Byte[] buffer = new byte[64];
                    int rec = n.Receive(buffer);

                    if (rec <= 0)
                    {
                        throw new SocketException();
                    }

                    //decodes them to strings and gives output with the incoming string
                    string newmessage = Encoding.Default.GetString(buffer);
                    newmessage = newmessage.Substring(0, newmessage.IndexOf("\0"));

                    if (nameEntered == false) //if the user has just connected and hasn't entered its name yet
                    {
                        clientName = newmessage; //takes the new message as client's name

                        if (userList.Contains(clientName)) //if already there is a client with that name, disconnect the client
                        {
                            byte[] bufferDisc = Encoding.Default.GetBytes("Username exists. You are being disconnected\n");
                            n.Send(bufferDisc);
                            n.Close();
                            socketList.Remove(n);
                            connected = false;
                            if (!terminating)
                                richTextBox1.Text += (clientName + " has disconnected...\n");
                            break;
                        }
                        else
                        {
                            userList.Add(clientName); //add the new client's name to 
                            scoreBoard.Add(0);
                            nameEntered = true;
                        }
                    }
                    else
                    {
                        richTextBox1.Text += (clientName + ": " + newmessage + "\r\n");
                    }


                    if (newmessage == "list") //if a client requests the list of the clients, send the list
                    {
                        string list = "";
                        int i;
                        foreach (string s in userList)
                        {
                            i = userList.IndexOf(s);
                            list = list + "\nName: " + s + " - Point: " + scoreBoard[i];
                        }

                        byte[] buffer2 = Encoding.Default.GetBytes(list);
                        n.Send(buffer2);
                     
                    }

                    if (newmessage.Length > 6 && newmessage.Substring(0, 6) == "invite") // eğer mesaj invitela başlarsa
                    {
                        invited = newmessage.Substring(7);

                        richTextBox1.Text += clientName + " invites " + invited + " for a game!\n";
                        if (userList.Contains(invited))
                        {
                            if (!(playingAtm.Contains(invited)) && !(prePlaying.Contains(invited)))
                            {
                                
                                invUser = userList.IndexOf(invited);

                                richTextBox1.Text += invUser;
                                richTextBox1.Text += curUser;
                                ikili bum;
                                bum.invited = invUser;
                                bum.inviter = curUser;
                                bum.invited_choice = "";
                                bum.inviter_choice = "";
                                bum.roundNo = 0;
                                bum.invited_score = 0;
                                bum.inviter_score = 0;
                                playList.Add(bum);
                                gameNo = playList.Count-1;

                                byte[] buffer2 = Encoding.Default.GetBytes("You are invited to play with " + invited + "\n");
                                socketList[invUser].Send(buffer2);
                                prePlaying.Add(userList[curUser]); // listeye önce kendini ekliyo
                                prePlaying.Add(userList[playList[gameNo].invited]); //Sonra diğerini
                            }

                            else
                            {
                                byte[] buffer2 = Encoding.Default.GetBytes(invited + " is not available for a game. \n");
                                socketList[curUser].Send(buffer2);
                            }
                        }
                    }

                    if (prePlaying.Contains(userList[curUser]) && !playingAtm.Contains(userList[curUser]) && newmessage == "accept")
                    {
                        int i = 0;
                        foreach(ikili s in playList)
                        {
                            if(s.invited == curUser)
                            {
                                gameNo = i;
                            }
                            i++;
                        } 


                        byte[] buffer2 = Encoding.Default.GetBytes("Game has started!\n");
                        socketList[curUser].Send(buffer2);
                        socketList[playList[gameNo].inviter].Send(buffer2);
                      
                        playingAtm.Add(userList[playList[gameNo].invited]);
                        playingAtm.Add(userList[playList[gameNo].inviter]);

                        prePlaying.Remove(userList[playList[gameNo].invited]);
                        prePlaying.Remove(userList[playList[gameNo].inviter]);

                    }

                    if (prePlaying.Contains(userList[curUser]) && newmessage == "reject")
                    {
                        int i = 0;
                        foreach (ikili s in playList)
                        {
                            if (s.invited == curUser)
                            {
                                gameNo = i;
                            }
                            i++;
                        }

                        prePlaying.Remove(userList[playList[gameNo].invited]);
                        prePlaying.Remove(userList[playList[gameNo].inviter]);
                        
                        byte[] buffer2 = Encoding.Default.GetBytes("You have been rejected!\n");
                        socketList[playList[gameNo].inviter].Send(buffer2);
                        playList.RemoveAt(gameNo);
                    }

                    if (playingAtm.Contains(userList[curUser]))
                    {
                        if (newmessage == "SURRENDER")
                        {
                            int winner;
                            if (playList[gameNo].invited == curUser)
                            {
                                winner = playList[gameNo].inviter;
                            }
                            else
                            {
                                winner = playList[gameNo].invited;
                            }

                            scoreBoard[winner]++;
                            byte[] buffer2 = Encoding.Default.GetBytes("Game Over. The winner is : " + userList[winner]);
                            socketList[playList[gameNo].invited].Send(buffer2);
                            socketList[playList[gameNo].inviter].Send(buffer2);
                            playingAtm.Remove(userList[playList[gameNo].invited]);
                            playingAtm.Remove(userList[playList[gameNo].inviter]);
                            playList.RemoveAt(gameNo);
                        }

                        else if (newmessage == "rock" || newmessage == "scissors" || newmessage == "paper")
                        {
                            String ch1;
                            String ch2;
                            //bunlar saçmalık
                            ikili newbum;
                            newbum.invited = playList[gameNo].invited;
                            newbum.inviter = playList[gameNo].inviter;

                            if (playList[gameNo].invited == curUser)
                            {
                                newbum.inviter_choice = playList[gameNo].inviter_choice;
                                newbum.invited_choice = newmessage;
                                ch1 = playList[gameNo].inviter_choice;
                                ch2 = newbum.invited_choice;
                            }

                            else
                            {
                                newbum.invited_choice = playList[gameNo].invited_choice;
                                newbum.inviter_choice = newmessage;
                                ch1 = playList[gameNo].invited_choice;
                                ch2 = newbum.inviter_choice;
                            }

                            newbum.roundNo = playList[gameNo].roundNo;
                            //newbum.invited_choice = "";
                            //newbum.inviter_choice = "";
                            newbum.invited_score = playList[gameNo].invited_score;
                            newbum.inviter_score = playList[gameNo].inviter_score;
                            playList[gameNo] = newbum;
                            //saçmalık bitti


                            if (ch1 != "")
                            {
                                //bunlar saçmalık
                                ikili newbum3;
                                newbum3.invited = playList[gameNo].invited;
                                newbum3.inviter = playList[gameNo].inviter;
                                newbum3.roundNo = playList[gameNo].roundNo + 1;
                                newbum3.inviter_choice = "";
                                newbum3.invited_choice = "";
                                String pl1 = playList[gameNo].invited_choice;
                                String pl2 = playList[gameNo].inviter_choice;

                                if (pl1 == pl2)
                                {
                                    newbum3.inviter_score = playList[gameNo].inviter_score;
                                    newbum3.invited_score = playList[gameNo].invited_score;
                                    byte[] buffer2 = Encoding.Default.GetBytes("Round over. Tie!");
                                    socketList[playList[gameNo].invited].Send(buffer2);
                                    socketList[playList[gameNo].inviter].Send(buffer2);
                                }

                                else
                                {
                                    if ((pl1 == "rock" && pl2 == "scissors") || (pl1 == "scissors" && pl2 == "paper") || (pl1 == "paper" && pl2 == "rock"))
                                    {
                                        newbum3.invited_score = playList[gameNo].invited_score + 1;
                                        newbum3.inviter_score = playList[gameNo].inviter_score;

                                        byte[] buffer2 = Encoding.Default.GetBytes("Round over. 1 point for " + userList[playList[gameNo].invited]);
                                        socketList[playList[gameNo].invited].Send(buffer2);
                                        socketList[playList[gameNo].inviter].Send(buffer2);
                                    }

                                    else if((pl1 == "scissors" && pl2 == "rock") || (pl1 == "paper" && pl2 == "scissors") || (pl1 == "rock" && pl2 == "paper"))
                                    {
                                        //ch2'ye bir puan 
                                        newbum3.inviter_score = playList[gameNo].inviter_score +1;
                                        newbum3.invited_score = playList[gameNo].invited_score ;

                                        byte[] buffer2 = Encoding.Default.GetBytes("Round over. 1 point for " + userList[playList[gameNo].inviter]);
                                        socketList[playList[gameNo].invited].Send(buffer2);
                                        socketList[playList[gameNo].inviter].Send(buffer2);
                                    }

                                    else
                                    {
                                        richTextBox1.Text += ("Mark 3\n");
                                        newbum3.inviter_score = playList[gameNo].inviter_score;
                                        newbum3.invited_score = playList[gameNo].invited_score;
                                    }

                                 }
                               
                                playList[gameNo] = newbum3;
                                    //saçmalık bitti                                    
                                }

                                if(playList[gameNo].inviter_score == 2 || playList[gameNo].invited_score == 2)
                                {
                                int winner;
                                    if (playList[gameNo].invited_score == 2)
                                    {
                                        winner = playList[gameNo].invited;
                                    }
                                    else
                                    {
                                        winner = playList[gameNo].inviter;
                                    }


                                    scoreBoard[winner]++;

                                    byte[] buffer2 = Encoding.Default.GetBytes("Game Over. The winner is : " + userList[winner]);
                                    socketList[playList[gameNo].invited].Send(buffer2);
                                    socketList[playList[gameNo].inviter].Send(buffer2);
                                    playingAtm.Remove(userList[playList[gameNo].invited]);
                                    playingAtm.Remove(userList[playList[gameNo].inviter]);
                                    playList.RemoveAt(gameNo);
                                    
                                    
                                }



                            }
                        }


                }
                catch
                {
                    if (!terminating)
                        richTextBox1.Text += (clientName + " has disconnected...\n");
                    int i = 0;
                    foreach (ikili s in playList)
                    {
                        if (s.invited == curUser)
                        {
                            gameNo = i;
                        }
                        i++;
                    }

                    ///BURADA PLAYINGATM DE Mİ DİYE BAKIP OYLEYSE DIGER ADAMI KAZANDIRSIN
                    if (playingAtm.Contains(userList[curUser]))
                    {
                        int winner;

                        if (playList[gameNo].invited == curUser)
                        {
                            winner = playList[gameNo].inviter;
                        }
                        else
                        {
                            winner = playList[gameNo].invited;
                        }

                        scoreBoard[winner]++;
                        byte[] buffer2 = Encoding.Default.GetBytes("Opponent left.Game Over. The winner is : " + userList[winner] + "\n");
                        socketList[winner].Send(buffer2);
                        playingAtm.Remove(userList[playList[gameNo].inviter]);
                        playingAtm.Remove(userList[playList[gameNo].invited]);
                        playList.RemoveAt(gameNo);
                    }

                    if(prePlaying.Contains(userList[curUser]))
                    {
                        int remaining;

                        if (playList[gameNo].invited == curUser)
                        {
                            remaining = playList[gameNo].inviter;
                        }
                        else
                        {
                            remaining = playList[gameNo].invited;
                        }

                        byte[] buffer2 = Encoding.Default.GetBytes("Opponent has disconnected.\n");
                        socketList[remaining].Send(buffer2);

                        prePlaying.Remove(userList[playList[gameNo].invited]);
                        prePlaying.Remove(userList[playList[gameNo].inviter]);

                        playList.RemoveAt(gameNo);
                    }
                    n.Close();
                    socketList.Remove(n);
                    int j = userList.IndexOf(clientName);
                    userList.Remove(clientName);
                    scoreBoard.RemoveAt(j);
                    connected = false;

                }
            }
        }

        //on button click starts the server with given port no
        private void button1_Click(object sender, EventArgs e)
        {
            serverPort = (int)numericUpDown1.Value; 

            try
            {
                //create a server with entered port no
                server.Bind(new IPEndPoint(IPAddress.Any, serverPort)); 
                richTextBox1.Text += ("Started listening for incoming connections.\n");

                //starts listening for incoming calls
                server.Listen(3); //the parameter here is maximum length of the pending connections queue

                //starts a thread with accept function
                thrAccept = new Thread(new ThreadStart(Accept));
                thrAccept.IsBackground = true;
                thrAccept.Start();
                listening = true;
            }
            catch
            {
                richTextBox1.Text += ("Cannot create a server with the specified port number\n Check the port number and try again.\n");
                richTextBox1.Text += ("terminating...\n");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            terminating = true;
            server.Close();
        }
    }

}
