using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Models
{
    /// <summary>
    /// 图形类型
    /// </summary>
    public enum  GraphType
    {
        Null = 1 << 1,
        /// <summary>
        /// 梯度
        /// </summary>
        Gradient=1<<2,
        /// <summary>
        /// 实时梯度
        /// </summary>
        RealGradient=1<<3,
        /// <summary>
        /// 速度
        /// </summary>
        Speed=1<<4,
        /// <summary>
        /// 收集区域
        /// </summary>
        Span=1<<5,
        /// <summary>
        /// 辅助线
        /// </summary>
        AssistLine=1<<6,
        /// <summary>
        /// 可拖动的线
        /// </summary>
        DraggableLine=1<<7,
        /// <summary>
        /// 叠加峰
        /// </summary>
        SuperimposedPeak=1<<8,
        /// <summary>
        /// 峰
        /// </summary>
        Peak = 1 << 9,
    }
}
