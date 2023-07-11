using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IconEditSvg
{
    class PathIt
    {
        internal enum SearchType { 
            ALL,
            NOT_Z,
            NOT_Z_M,
        };

        List<SvgPathItem> _path;
        int _index;

        internal PathIt(List<SvgPathItem> path, int index) 
        {
            _path = path;
            _index = index;
        }

        internal SvgPathItem GetItem()
        {
            return _path[_index];
        }

        internal PathIt GetNext(SearchType type,bool befor)
        {
            SvgPathItem item = null;
            var step = befor ? -1 : 1;
            var index = _index;
            while (true)
            {
                index += step;
                if (index < 0)
                {
                    index = _path.Count - 1;
                    item = _path[index];
                    if (!item.IsZ())
                    {
                        return null;
                    }
                }
                else if (index >= _path.Count)
                {
                    if (!item.IsZ())
                    {
                        return null;
                    }
                    index = 0;
                    item = _path[index];
                }
                else
                {
                    item = _path[index];
                }
                if (item.IsZ() && (type == SearchType.NOT_Z || type == SearchType.NOT_Z_M))
                {
                    continue;
                }
                else if (item.IsM() && (type == SearchType.NOT_Z_M))
                {
                    continue;
                }
                return new PathIt(_path, index);
            }
        }

        internal PathIt NextC(bool befor) 
        {
            if (_path.Count < 4) return null;
            if (befor)
            {
                var index = _index;
                index--;
                if (index < 0) return null; // ありえないけど
                var item = _path[index];
                if (item.IsC()) return new PathIt(_path, index);
                if (!item.IsM()) return null;
                var p = item.GetPoint(0);
                if (index != 0) return null;
                index = _path.Count - 1;
                item = _path[index];
                if (!item.IsZ()) return null;
                index--;
                item = _path[index];
                if (!item.IsC()) return null;
                var p2 = item.GetPoint(SvgPathItem.POS_C_END);
                if (p != p2) return null;
                return new PathIt(_path, index);
            }
            else 
            {
                var index = _index;
                index ++;
                if (index >= _path.Count) {
                    return null;
                }
                var item = _path[index];
                if (item.IsC()) {
                    return new PathIt(_path, index);
                } else if (!item.IsZ()) {
                    return null;
                }
                index = 0;
                item = _path[index];
                if (!item.IsM()) return null;

                var p = GetItem().GetPoint(SvgPathItem.POS_C_END);
                var p2 = item.GetPoint(0);
                if (p != p2) return null;


                index++;
                item = _path[index];
                if (item.IsC()) return new PathIt(_path, index);
            }
            return null;
        }
    }
}
