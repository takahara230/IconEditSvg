using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconEditSvg
{
    public class UndoData
    {
        public UndoData(string _text,SvgPathData.SvgPathIndex _index) {
            svgText = _text;
            index = _index;
        }
        string svgText;
        SvgPathData.SvgPathIndex index;

        public string GetSvgText() {
            return svgText;
        }
        public SvgPathData.SvgPathIndex GetIndex() {
            return index;
        }
    }
}
