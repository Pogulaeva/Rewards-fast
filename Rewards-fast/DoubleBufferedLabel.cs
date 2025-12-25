using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rewards_fast
{
    public class DoubleBufferedLabel : Label
    {
        public DoubleBufferedLabel()
        {
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint, true);
        }
    }
}
