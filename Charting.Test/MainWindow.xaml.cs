using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Charting.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval=TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            DispatcherTimer timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromSeconds(10);
            timer1.Tick += Timer_Tick1;
            timer1.Start();


            LastTime = 50;

            #region 
            GradientX = new double[LastTime];
            GradientY = new double[LastTime];
            SpeedX = new double[LastTime];
            SpeedY = new double[LastTime];

            GradientRealX=new double[OscillogramCharting.AllNumConst];
            GradientRealY=new double[OscillogramCharting.AllNumConst];

            SpeedRealX = new double[OscillogramCharting.AllNumConst];
            SpeedRealY = new double[OscillogramCharting.AllNumConst];

            PressureX = new double[OscillogramCharting.AllNumConst];
            PressureY = new double[OscillogramCharting.AllNumConst];

            for (int i = 0; i < LastTime; i++)
            {
                SpeedX[i] = i * 2;
                SpeedY[i] = i % 5 * 2*i;

                GradientX[i]= i*2;
                GradientY[i]= i%3*i;
            }
            #endregion
        }

        private void Timer_Tick1(object? sender, EventArgs e)
        {
            //LastTime++;
            GradientX = new double[LastTime];
            GradientY = new double[LastTime];
            SpeedX = new double[LastTime];
            SpeedY = new double[LastTime];
            for (int i = 0; i < LastTime; i++)
            {
                SpeedX[i] = i * 2;
                SpeedY[i] = i % 5 * 2 * i;

                GradientX[i] = i * 2;
                GradientY[i] = i % 3 * i;
            }
        }

        int x = 0;
        private void Timer_Tick(object? sender, EventArgs e)
        {
            for (int i = 0; i < LastTime; i++)
            {
                //GradientX[i] = i;
                //GradientY[i] = i % 3 * x++;
            }
            CurrentTime++;
        }


     

        private int currentTime;

        public int CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTime"));
            }
        }

        private int lastTime;

        public int LastTime
        {
            get { return lastTime; }
            set
            {
                lastTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastTime"));
            }
        }

        private double[] gradientX;

        public double[] GradientX
        {
            get { return gradientX; }
            set { gradientX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientX"));
            }
        }

        private double[] gradientY;

        public double[] GradientY
        {
            get { return gradientY; }
            set
            {
                gradientY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientY"));
            }
        }



        private double[] speedX;

        public double[] SpeedX
        {
            get { return speedX; }
            set
            {
                speedX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedX"));
            }
        }

        private double[] speedY;

        public double[] SpeedY
        {
            get { return speedY; }
            set
            {
                speedY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedY"));
            }
        }



        private double[] gradientRealX;

        public double[] GradientRealX
        {
            get { return gradientRealX; }
            set
            {
                gradientRealX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientRealX"));
            }
        }

        private double[] gradientRealY;

        public double[] GradientRealY
        {
            get { return gradientRealY; }
            set
            {
                gradientRealY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientRealY"));
            }
        } 
        
        
        private double[] speedRealX;

        public double[] SpeedRealX
        {
            get { return speedRealX; }
            set
            {
                speedRealX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedRealX"));
            }
        }

        private double[] speedRealY;

        public double[] SpeedRealY
        {
            get { return speedRealY; }
            set
            {
                speedRealY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedRealY"));
            }
        }
        
        
        private double[] pressureX;

        public double[] PressureX
        {
            get { return pressureX; }
            set
            {
                pressureX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PressureX"));
            }
        }

        private double[] pressureY;

        public double[] PressureY
        {
            get { return pressureY; }
            set
            {
                pressureY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PressureY"));
            }
        }
    }
}