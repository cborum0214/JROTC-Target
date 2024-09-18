using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingApp
{
    public partial class Form1 : Form
    {
        // List of IP addresses to ping
        private string[] ipAddresses = {
        "172.17.96.147",    // Monitor 1
        "172.17.96.146",    // Monitor 2
        "172.17.96.156",    // Monitor 3
        "172.17.96.154",    // Monitor 4
        "172.17.96.149",    // Target 1
        "172.17.96.151",    // Target 2
        "172.17.96.155",    // Target 3
        "172.17.96.153",    // Target 4
        "172.17.96.157",    // Monitor 5
        "172.17.96.160",    // Monitor 6
        "172.17.96.162",    // Monitor 7
        "172.17.96.164",    // Monitor 8
        "172.17.96.158",    // Target 5
        "172.17.96.159",    // Target 6
        "172.17.96.161",    // Target 7
        "172.17.96.163",    // Target 8
        "172.17.96.152",    // Display 1
        "172.17.96.150"     // Router
    };

        // Custom display names for the IP addresses
        private string[] displayNames = {
        "Monitor 1",
        "Monitor 2",
        "Monitor 3",
        "Monitor 4",
        "Target 1",
        "Target 2",
        "Target 3",
        "Target 4",
        "Monitor 5",
        "Monitor 6",
        "Monitor 7",
        "Monitor 8",
        "Target 5",
        "Target 6",
        "Target 7",
        "Target 8",
        "Display 1",
        "Router"
    };

        private Panel[] panels;
        private Label[] statusLabels;
        private Button startButton, stopButton;
        private CancellationTokenSource cts;

        public Form1()
        {
            InitializeComponent();
            InitializePanels();
            InitializeButtons();
            AdjustFormSize(); // Call method to adjust the form size based on the layout
        }

        // Method to initialize the colored panels for IP status
        private void InitializePanels()
        {
            panels = new Panel[ipAddresses.Length];
            statusLabels = new Label[ipAddresses.Length];

            for (int i = 0; i < ipAddresses.Length; i++)
            {
                // Create panel for each IP address
                Panel panel = new Panel
                {
                    Size = new Size(150, 90), // Increased height to fit all labels
                    BackColor = Color.Gray,
                    Location = new Point(10 + (i % 4) * 170, 10 + (i / 4) * 110)
                };

                // Custom display name label
                Label displayNameLabel = new Label
                {
                    Text = displayNames[i],
                    Location = new Point(10, 10),
                    AutoSize = true
                };

                // Label for the IP address
                Label ipLabel = new Label
                {
                    Text = ipAddresses[i],
                    Location = new Point(10, 30),
                    AutoSize = true
                };

                // Status label that will be updated based on ping result
                Label statusLabel = new Label
                {
                    Text = "Waiting...",
                    Location = new Point(10, 50),
                    AutoSize = true
                };

                // Add the labels to the panel
                panel.Controls.Add(displayNameLabel);
                panel.Controls.Add(ipLabel);
                panel.Controls.Add(statusLabel);

                // Add the panel to the form
                this.Controls.Add(panel);

                // Store references to the panels and labels
                panels[i] = panel;
                statusLabels[i] = statusLabel;
            }
        }

        // Method to initialize Start and Stop buttons
        private void InitializeButtons()
        {
            startButton = new Button
            {
                Text = "Start",
                Location = new Point(10, 560),
                Size = new Size(100, 30)
            };
            startButton.Click += StartButton_Click;

            stopButton = new Button
            {
                Text = "Stop",
                Location = new Point(120, 560),
                Size = new Size(100, 30),
                Enabled = false // Initially disabled
            };
            stopButton.Click += StopButton_Click;

            this.Controls.Add(startButton);
            this.Controls.Add(stopButton);
        }

        // Event handler for Start button
        private void StartButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            stopButton.Enabled = true;

            // Create a new CancellationTokenSource
            cts = new CancellationTokenSource();
            StartPinging(cts.Token);
        }

        // Event handler for Stop button
        private void StopButton_Click(object sender, EventArgs e)
        {
            stopButton.Enabled = false;
            startButton.Enabled = true;

            // Cancel the pinging process
            cts.Cancel();
        }

        // Start the pinging process and recheck every 5 seconds
        private void StartPinging(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var pingTasks = new Task[ipAddresses.Length];

                    // Pinging all IP addresses simultaneously
                    for (int i = 0; i < ipAddresses.Length; i++)
                    {
                        int index = i; // Capture the correct index for the Task
                        pingTasks[i] = Task.Run(() => PingAddress(ipAddresses[index], index));
                    }

                    // Wait for all pings to complete
                    await Task.WhenAll(pingTasks);

                    // Recheck every 5 seconds
                    await Task.Delay(5000);
                }
            }, cancellationToken);
        }

        // Method to ping a specific address and update the corresponding panel and label
        private void PingAddress(string ipAddress, int index)
        {
            Ping pingSender = new Ping();
            try
            {
                PingReply reply = pingSender.Send(ipAddress, 1000); // 1 second timeout
                if (reply.Status == IPStatus.Success)
                {
                    UpdatePanelAndLabel(panels[index], statusLabels[index], Color.Green, "Success");
                }
                else
                {
                    UpdatePanelAndLabel(panels[index], statusLabels[index], Color.Red, "Failed");
                }
            }
            catch
            {
                UpdatePanelAndLabel(panels[index], statusLabels[index], Color.Red, "Error");
            }
        }

        // Method to update the panel color and status label on the UI thread
        private void UpdatePanelAndLabel(Panel panel, Label statusLabel, Color color, string statusText)
        {
            if (panel.InvokeRequired)
            {
                panel.Invoke(new Action(() =>
                {
                    panel.BackColor = color;
                    statusLabel.Text = statusText;
                }));
            }
            else
            {
                panel.BackColor = color;
                statusLabel.Text = statusText;
            }
        }

        // Method to adjust the form size based on the panel layout
        private void AdjustFormSize()
        {
            int panelWidth = 150;  // Each panel's width
            int panelHeight = 90;  // Each panel's height
            int padding = 20;      // Padding between panels and form edges

            // Calculate the required width and height based on the number of panels and layout
            int columns = 4; // Number of columns in which panels are laid out
            int rows = (int)Math.Ceiling(ipAddresses.Length / (double)columns); // Rows needed to fit all panels

            // Calculate form width and height
            int formWidth = (panelWidth + padding) * columns + padding;
            int formHeight = (panelHeight + padding) * rows + padding + 100; // Extra space for buttons

            // Set the form size
            this.Size = new Size(formWidth, formHeight);
        }
    }
}
