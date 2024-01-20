using Charting.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Extensions
{
    public static class OscillogramExtension
    {
        /// <summary>
        /// 生成XY
        /// </summary>
        /// <param name="waves">初始波长</param>
        /// <returns></returns>
        public static (ObservableCollection<OscillogramWave> X, ObservableCollection<OscillogramWave> Y) InitilzeXY(  params double[] waves)
        {
            var X = new ObservableCollection<OscillogramWave>();
            var Y = new ObservableCollection<OscillogramWave>();

            foreach (var wave in waves)
            {
                var dscib = $"{wave}";
                var count = X.Count(_ => _.Wave == wave);
                if (count > 0)
                    dscib = $"{wave}({count})";
                OscillogramWave key = new OscillogramWave(wave) { Color = OscillogramChartingCore.ColorHtmls[X.Count()], Decription = dscib, IsSelected = true };
                var x = (OscillogramWave)key.Clone();
                x.V = new double[OscillogramCharting.AllNumConst];
                var y = (OscillogramWave)key.Clone();
                y.V = new double[OscillogramCharting.AllNumConst];
                X.Add(x);
                Y.Add(y);
            }
            return (X, Y);
        }
        /// <summary>
        /// 追加XY
        /// </summary>
        /// <param name="source">资源集合</param>
        /// <param name="wave">追加波长</param>
        /// <param name="x">x集合</param>
        /// <param name="y">y集合</param>
        public static void AppendXY(this (ObservableCollection<OscillogramWave> X, ObservableCollection<OscillogramWave> Y) source,double wave, double[] x, double[] y)
        {
            var dscib = $"{wave}";
            var count = source.X.Count(_ => _.Wave == wave);
            if (count > 0)
                dscib = $"{wave}({count})";

            OscillogramWave key = new OscillogramWave(wave) { Color = OscillogramChartingCore.ColorHtmls[source.X.Count], Decription = dscib, IsSelected = true };

            var _x = (OscillogramWave)key.Clone();
            _x.V = new double[OscillogramCharting.AllNumConst];
            Array.Copy(x, _x.V, x.Count());

            var _y = (OscillogramWave)key.Clone();
            _y.V = new double[OscillogramCharting.AllNumConst];
            Array.Copy(y, _y.V, y.Count());

            source.X.Add(_x);
            source.Y.Add(_y);
        }
        /// <summary>
        /// 删除XY
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        public static void Remove(this (ObservableCollection<OscillogramWave> X, ObservableCollection<OscillogramWave> Y) source, OscillogramWave key)
        {
            source.X.Remove(key);
            source.Y.Remove(key);
        }


    }
}
