using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

public class WeightCounter
{
    private List<float> _weightSumed;
    private int _count = 0;
    public int Count { get => _count; }
    public void Add(List<float> weight)
    {
        if (_weightSumed != null && _weightSumed.Count != weight.Count)
        {
            throw new Exception("_weightSumed and weight must have save count");
        }

        // first time
        if (_weightSumed == null)
        {
            _weightSumed = new List<float>();
            for (int i = 0; i < weight.Count; i++)
            {
                _weightSumed.Add(0f);
            }
        }

        float currSum = 0;
        for (int i = 0; i < _weightSumed.Count; i++)
        {
            _weightSumed[i] += weight[i];
            currSum += weight[i];
        }

        // if all the elements in weight are zero, do not count++
        if (currSum > 0)
        {
            _count++;
        }
    }
    public List<float> Average()
    {
        if (_weightSumed != null && _count != 0)
            for (int i = 0; i < _weightSumed.Count; i++)
                _weightSumed[i] /= _count;
        return _weightSumed;
    }

}
