using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class RandomSpawner : MonoBehaviour
{
    [Min(100)] public int spawnCount = 10000;
    public bool useSTDRandom = true;

    private Texture2D _mainTex;
    private Dictionary<Vector2, int> _randomPoints = new Dictionary<Vector2, int>();

    struct PointProbility {
        public Vector2 point;
        public double probility;
        public int count;

        public PointProbility(Vector2 po, int cnt, double pr) {
            point = po;
            count = cnt;
            probility = pr;
        }
    };

    private void OnEnable() {
        if (_mainTex == null) {
            _mainTex = new Texture2D(256, 256, TextureFormat.RGBA32, 0, true);
            GetComponent<Renderer>().sharedMaterial.mainTexture = _mainTex;
        }

        Debug.Assert(_mainTex);

        Reset();
        RandomSpawn();
        VisualizeRandomPoints();
        DumpProbabilities();
    }

    private void Reset() {
        for (int i=0; i<_mainTex.width; i++) {
            for (int j=0; j<_mainTex.height; j++) {
                _mainTex.SetPixel(i, j, Color.black, 0);
            }
        }

        _mainTex.Apply();
        _randomPoints.Clear();
    }

    
    Vector2 VecInsideUnitCircle(bool useSTD) {
        if (useSTD)
            return Random.insideUnitCircle;

        float radius = Random.Range(0f, 1f);
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    
    void RandomSpawn() {
        float radius = _mainTex.width / 2f;
        Vector2 center = new Vector2(radius, radius);
        for (int i=0; i<spawnCount;  i++) {
            Vector2 p = VecInsideUnitCircle(useSTDRandom) * radius;
            p += center;
            if (_randomPoints.ContainsKey(p)) {
                _randomPoints[p]++;
            } else {
                _randomPoints.Add(p, 1);
            }
        }
    }

    
    void VisualizeRandomPoints() {
        foreach(var point in _randomPoints) {
            _mainTex.SetPixel((int)point.Key.x, (int)point.Key.y, Color.blue);
        }
        _mainTex.Apply();
    }


    void DumpProbabilities() {
        List<PointProbility> pp = new List<PointProbility>(_randomPoints.Count);

        foreach(var point in _randomPoints) {
            pp.Add(new PointProbility(point.Key, point.Value ,point.Value / (double)spawnCount));
        }

        pp.Sort((lhs, rhs) => { 
            if(lhs.probility == rhs.probility)
                return 0;
            return lhs.probility > rhs.probility ? 1 : -1;
        });
        
        foreach(var _pp in pp) {
            Debug.LogFormat("<{0}, {1}>: {2} / {3}", _pp.point.x, _pp.point.y, _pp.count, _pp.probility);
        }
    }


}
