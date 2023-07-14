using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconEditSvg
{
    public class UndoData
    {
        public UndoData(string _text,int selectElement, SvgPathData.SvgPathIndex _index) {
            svgText = _text;
            index = _index;
            this.selectElement = selectElement;
        }
        string svgText;
        SvgPathData.SvgPathIndex index;
        int selectElement = -1;

        public string GetSvgText() {
            return svgText;
        }
        public SvgPathData.SvgPathIndex GetIndex() {
            return index;
        }

        public int GetElementIndex() {
            return selectElement;
        }
    }
}
