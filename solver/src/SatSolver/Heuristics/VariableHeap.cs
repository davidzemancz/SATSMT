namespace SatSolver.Heuristics;

// =====================================================================
//  VariableHeap - indexovana binarni halda (max-heap) nad aktivitami
// =====================================================================
// VSIDS potrebuje rychle:
//   - vybrat promennou s nejvyssi aktivitou  (RemoveMax)
//   - zvednout aktivitu promenne a opravit pozici  (Increase)
//   - vratit promennou do haldy po backtrackingu  (Insert)
// Klasicka binarni halda v poli + jeste navic pole _position kde si pamatuju
// kde ktera promenna v halde lezi (aby sel Increase v O(log n) a ne O(n)).
// V MiniSatu se temuhle rika "order heap". Bez ni by byl VSIDS pomaly jak blecha.
public sealed class VariableHeap
{
    private readonly int[] _heap;        // _heap[i] = promenna na pozici i
    private readonly int[] _position;    // _position[v] = pozice promenne v v halde, -1 = neni v halde
    private readonly double[] _activity; // odkaz na pole aktivit (sdilim ho s VSIDS)
    private int _size;

    public VariableHeap(int varCount, double[] activity)
    {
        _heap = new int[varCount + 1];
        _position = new int[varCount + 1];
        Array.Fill(_position, -1);
        _activity = activity;
        _size = 0;
    }

    public bool IsEmpty => _size == 0;

    private bool Contains(int var) => _position[var] >= 0;

    public void Insert(int var)
    {
        if (Contains(var))
            return; // uz tam je, nechci ji tam mit dvakrat
        _position[var] = _size;
        _heap[_size] = var;
        _size++;
        SiftUp(_position[var]);
    }

    // Aktivita promenne vzrostla -> probublej ji v halde nahoru.
    public void Increase(int var)
    {
        if (Contains(var))
            SiftUp(_position[var]);
    }

    // Vyndej a vrat promennou s nejvyssi aktivitou (koren haldy).
    public int RemoveMax()
    {
        int top = _heap[0];
        _position[top] = -1;
        _size--;
        if (_size > 0)
        {
            // posledni prvek dam na vrchol a probublam ho dolu
            _heap[0] = _heap[_size];
            _position[_heap[0]] = 0;
            SiftDown(0);
        }
        return top;
    }

    // Probublani nahoru: dokud je rodic mensi, prohazuj.
    private void SiftUp(int i)
    {
        int var = _heap[i];
        double act = _activity[var];
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_activity[_heap[parent]] >= act)
                break;
            _heap[i] = _heap[parent];
            _position[_heap[i]] = i;
            i = parent;
        }
        _heap[i] = var;
        _position[var] = i;
    }

    // Probublani dolu: porad se zanoruj k vetsimu z potomku.
    private void SiftDown(int i)
    {
        int var = _heap[i];
        double act = _activity[var];
        while (true)
        {
            int left = 2 * i + 1;
            if (left >= _size)
                break;
            int right = left + 1;
            // vyber vetsiho z potomku (kdyz pravy existuje a je vetsi nez levy)
            int child = (right < _size && _activity[_heap[right]] > _activity[_heap[left]]) ? right : left;
            if (_activity[_heap[child]] <= act)
                break;
            _heap[i] = _heap[child];
            _position[_heap[i]] = i;
            i = child;
        }
        _heap[i] = var;
        _position[var] = i;
    }
}
