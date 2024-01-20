using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Charting.Models
{
    public class OscillogramWave : INotifyPropertyChanged, IEquatable<OscillogramWave>,ICloneable
    {
        private double wave;
        private string? description;
        private bool isSelected;
        private string? color;

        public event PropertyChangedEventHandler? PropertyChanged;
        private double[] v;

        public double[] V
        {
            get { return v; }
            set
            {
                v = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("V"));
            }
        }

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
        public OscillogramWave()
        {
            V=new double[OscillogramCharting.AllNumConst];
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
            V = new double[OscillogramCharting.AllNumConst];
        }

        public static implicit operator OscillogramWave(double d) => new(d);
        public override bool Equals(object? obj)
            => this.Equals(obj as OscillogramWave);

        public bool Equals(OscillogramWave? other)
        {
            if (other is null)
                return false;
            if (Object.ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return Wave == other.Wave && Decription == other.Decription;
        }

        public override int GetHashCode()
            => (Wave, Decription).GetHashCode();

        public object Clone()
            =>new OscillogramWave(wave, Decription!) {  V=V, Color=Color, IsSelected=IsSelected};

        public static bool operator ==(OscillogramWave l1, OscillogramWave l2)
        {
            if (l1 is null)
            {
                if (l2 is null)
                {
                    return true;
                }
                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return l1.Equals(l2);
        }
        public static bool operator !=(OscillogramWave l1, OscillogramWave l2)
            => !(l1 == l2);
    }
}
