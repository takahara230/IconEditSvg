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
    }
}
