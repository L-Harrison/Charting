using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Models
{
    public class OscillogramWave : INotifyPropertyChanged
    {
        private double wave;
        private string? description;
        private bool isSelected;
        private string? color;

        public event PropertyChangedEventHandler? PropertyChanged;
        public double Wave
        {
            get { return wave; }
            set
            {
                wave = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Wave"));
            }
        }
        public string? Decription
        {
            get { return description; }
            set
            {
                description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Decription"));
            }
        }
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
            }
        }
        public string? Color
        {
            get { return color; }
            set
            {
                color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }
        public OscillogramWave(double wave, string decription = default!)
        {
            Wave = wave;
            Color = default!;
            IsSelected = false;
            if (decription == default)
                Decription = wave.ToString();
            else
                Decription = decription;
        }

        public static implicit operator OscillogramWave(double d) => new (d);

    }
}
