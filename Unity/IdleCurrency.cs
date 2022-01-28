
/*
* 2020.12 - 최종 단위의 float 값만 가지던 초안
* 2021.12 - 퇴사 후 코드 제출용으로 각종 명칭 등 코드 제출 시 문제될만한 부분 제거
* 2022.01 - 최종 단위 ~ 최종 단위 - 5 까지 값을 보관하고 그에 따른 연산함수 수정 및 음수 처리 부분 변경
* TODO : 언리얼 및 C++ 학습용으로 C++로도 구현 및 적용
* TODO : 보관할 단위 갯수 지정으로 간다면 단위 별 값 저장 컨테이너를 다른 가벼운 자료구조로 변경
*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NUM_TYPE = System.Int32;
using CURRENCY_TYPE = CurrencyType;

public enum CurrencyType : int
{
    Idle_Cash,
    Idle_Gold,
};

[System.Serializable]
public class IdleCurrency
{
    #region CONST, STATIC
    /// <summary>
    /// 무한 루프 방지, 에러체크용 
    /// </summary>
    const int MAX_LOOP_COUNT = 10000;
    /// <summary>
    /// 단위 숫자 자릿수
    /// </summary>
    const int NUM_DIGITS = 100;
    /// <summary>
    /// 보관할 단위 갯수
    /// </summary>
    const int MAX_GRADE_DIFF = 5;
    /// <summary>
    /// 단위 당 최대값
    /// </summary>
    const NUM_TYPE NUM_MAX = NUM_DIGITS * 10 - 1;
    #endregion

    #region ariables
    /// <summary>
    /// 재화 타입
    /// </summary>
    public CURRENCY_TYPE curreny_type { get; private set; }
    /// <summary>
    /// 최종 단위
    /// </summary>
    public int unit; // 단위, (NUM_DIGIT * 10) ^ killo_count
    /// <summary>
    /// 단위 별 값들
    /// </summary>
    Dictionary<int, NUM_TYPE> values = new Dictionary<int, NUM_TYPE>() { { 0, 0 } }; // TODO : 보관 갯수 제한을 둘꺼면 다른 자료구조 사용하는게 좋을거같음
    /// <summary>
    /// 음수 체크
    /// </summary>
    bool isNegative = false;
    #endregion

    #region GET SET
    /// <summary>
    /// get => 마지막 두 단위를 사용하여 000.00f 형식의 값 출력,
    /// set => 입력받은 값을 최종 단위 값에 적용
    /// </summary>
    public float num
    {
        get => last_num + (second_last_num / 10) / 100f;
        set
        {
            values.Clear();
            SetCurrencyNumList(value);
        }
    }
    /// <summary>
    /// get => 최종 단위 값
    /// </summary>
    public NUM_TYPE last_num { get => values == null || values.Count == 0 || !values.ContainsKey(unit) ? 0 : values[unit]; }
    /// <summary>
    /// get => 최종 이전 단위 값
    /// </summary>
    public NUM_TYPE second_last_num { get => values == null || values.Count == 0 || !values.ContainsKey(unit - 1) ? 0 : values[unit - 1]; }
    #endregion

    #region 생성자 및 데이터 정리
    /// <param name="_ic">복사할 원본</param>
    public IdleCurrency(IdleCurrency _ic)
    {
        curreny_type = _ic.curreny_type;
        isNegative = _ic.isNegative;
        values = new Dictionary<int, NUM_TYPE>(_ic.values);
        unit = _ic.unit;
        RefreshCurrencyNumList();
    }
    /// <param name="_type">재화타입</param>
    public IdleCurrency(CURRENCY_TYPE _type = CURRENCY_TYPE.Idle_Gold)
    {
        curreny_type = _type;
        unit = 0;
    }
    /// <param name="_input">단위값을 포함하지 않는 값</param>
    /// <param name="_type">재화타입</param>
    public IdleCurrency(double _input, CURRENCY_TYPE _type = CURRENCY_TYPE.Idle_Gold)
    {
        curreny_type = _type;
        if (_input == 0)
            return;

        SetCurrencyNumList(_input);
    }

    //public IdleCurrency(System.IComparable _input) : this((double)_input) { }

    /// <param name="_num">해당 단위의 값</param>
    /// <param name="_unit">단위</param>
    /// <param name="_type">재화타입</param>
    public IdleCurrency(float _num, int _unit, CURRENCY_TYPE _type = CURRENCY_TYPE.Idle_Gold)
    {
        curreny_type = _type;
        if (_num == 0)
            return;
        unit = _unit;

        SetCurrencyNumList(_num);
    }
    /// <param name="_num">해당 단위의 값</param>
    /// <param name="_unit_str">문자열형 단위</param>
    /// <param name="_type">재화타입</param>
    public IdleCurrency(float _num, string _unit_str, CURRENCY_TYPE _type = CURRENCY_TYPE.Idle_Gold)
    {
        curreny_type = _type;
        if (_num == 0)
            return;
        if (string.IsNullOrEmpty(_unit_str) || int.TryParse(_unit_str, out int _output) == true /*|| _output == 0*/)
            unit = 0;
        else
        {
            for(int i = 0; i < _unit_str.Length; i++)
            {
                var _index = (_unit_str.Length - 1) - i;
                unit += (int)System.Math.Pow(26, _index) * (_unit_str[i] - 'a' + 1);
            }
        }

        SetCurrencyNumList(_num);
    }
    /// <summary>
    /// values 초기화 및 입력된 unit 단위부터 값 입력
    /// </summary>
    /// <param name="_num">현재 단위의 값</param>
    void SetCurrencyNumList(float _num)
    {
        int loop_count = 0;
        values.Clear();
        while (_num > NUM_MAX)
        {
            _num /= NUM_DIGITS * 10;
            unit++;
            if (_num <= NUM_MAX)
            {
                break;
            }

            loop_count++;
            if (loop_count >= MAX_LOOP_COUNT)
            {
                Debug.LogError("최대 루프 횟수 도달\n_num = " + _num + "\n_killo_count : " + unit + "\n_type : " + curreny_type.ToString());
                break;
            }
        }
        int _integer = Mathf.FloorToInt(_num);
        values.Add(unit, (NUM_TYPE)_integer);
        if (unit > 1)
            values.Add(unit - 1, (NUM_TYPE)Mathf.FloorToInt((_num - _integer) * NUM_DIGITS * 10));
    }
    /// <summary>
    /// values 초기화 및 입력된 unit 단위부터 값 입력
    /// </summary>
    /// <param name="_input">현재 단위의 값</param>
    void SetCurrencyNumList(double _input)
    {
        int loop_count = 0;
        values.Clear();
        while (_input > NUM_MAX)
        {
            _input /= NUM_DIGITS * 10;
            unit++;
            if (_input <= NUM_MAX)
            {
                break;
            }

            loop_count++;
            if (loop_count >= MAX_LOOP_COUNT)
            {
                Debug.LogError("최대 루프 횟수 도달\n_num = " + _input + "\n_killo_count : " + unit + "\n_type : " + curreny_type.ToString());
                break;
            }
        }
        float temp = (float)_input;
        int _integer = Mathf.FloorToInt(temp);
        values.Add(unit, (NUM_TYPE)_integer);
        if (unit > 1)
            values.Add(unit - 1, (NUM_TYPE)Mathf.FloorToInt((temp - _integer) * NUM_DIGITS * 10));
    }
    /// <summary>
    /// values 정리 
    /// </summary>
    void RefreshCurrencyNumList()
    {
        if (values == null || values.Count == 0) return;

        for (int _index = 0; _index <= unit; _index++)
        {
            if (!values.ContainsKey(_index)) continue;

            if (unit - _index > MAX_GRADE_DIFF || values[_index] == 0) //빈값이거나 단계 차이가 많이 나는 요소는 제거
            {
                if (_index == unit && unit > 0) unit--;
                values.Remove(_index);
                continue;
            }
            RefreshCurrencyNum(_index);
        }
    }
    /// <summary>
    /// values[_index] 정리
    /// </summary>
    /// <param name="_index">Index</param>
    void RefreshCurrencyNum(int _index)
    {
        if (values[_index] > NUM_MAX)
        {
            if (unit == _index)
                unit++;
            if (!values.ContainsKey(_index + 1))
                values.Add(_index + 1, 0);
            values[_index + 1] += (NUM_TYPE)(values[_index] / (NUM_DIGITS * 10));
            values[_index] %= NUM_DIGITS * 10;
            if (values[_index + 1] > NUM_MAX)
                RefreshCurrencyNum(_index + 1);
        }
    }
    /// <summary>
    /// 두 IdleCurrency 데이터 결합
    /// </summary>
    /// <param name="_ic">결합할 IdleCurrency</param>
    void CombineNumList(IdleCurrency _ic)
    {
        unit = Mathf.Max(unit, _ic.unit);
        var _ic_key_array = _ic.values.Keys.ToArray();
        foreach (int _index in _ic_key_array)
        {
            if (!values.Keys.Contains(_index))
            {
                values.Add(_index, _ic.values[_index] * (isNegative == _ic.isNegative ? 1 : -1));
            }
            else
            {
                values[_index] += _ic.values[_index] * (isNegative == _ic.isNegative ? 1 : -1);
            }
        }

        if (last_num == 0 && unit > 0)
        {
            values.Remove(unit);
            unit--;
        }

        if (last_num < 0)
        {
            //최종 값이 음수이므로 isNegative값 변경 뒤 각 단위 값들 단위 반전시키고 계산

            isNegative = !isNegative;

            for(int _index = 0; _index <= unit; _index++)
            {
                if (!values.ContainsKey(_index)) continue;

                if (values[_index] < 0)
                {
                    values[_index] *= -1;
                }
                else
                {
                    values[_index] = NUM_MAX - values[_index];
                    int next_idx = _index + 1;
                    if (next_idx <= unit)
                    {
                        if (values.Keys.Contains(next_idx))
                        {
                            values[next_idx] += 1;
                        }
                        else
                        {
                            values.Add(next_idx, 1);
                        }
                    }
                }
            }
        }
        else
        {
            for (int _index = 0; _index <= unit; _index++)
            {
                if (!values.ContainsKey(_index)) continue;

                if (values[_index] < 0)
                {
                    values[_index] = NUM_MAX + values[_index];
                    int next_idx = _index + 1;
                    if (next_idx <= unit)
                    {
                        if (values.Keys.Contains(next_idx))
                        {
                            values[next_idx] -= 1;
                        }
                        else
                        {
                            values.Add(next_idx, -1);
                        }
                    }
                }
            }
        }

        RefreshCurrencyNumList();
    }
    #endregion

    #region 연산자 및 static
    public static readonly IdleCurrency Zero = new IdleCurrency();

    public static IdleCurrency operator -(IdleCurrency A) { A.isNegative = true; return A; }
    public static IdleCurrency operator +(IdleCurrency A, IdleCurrency B)
    {
        if (A.curreny_type != B.curreny_type)
        {
            throw new System.ArgumentException("다른 타입끼리 비교를 시도함 " + A.curreny_type + " != " + B.curreny_type);
        }
        var _temp = new IdleCurrency(A);

        _temp.CombineNumList(B);

        return _temp;
    }    
    public static IdleCurrency operator -(IdleCurrency A, IdleCurrency B) => A + (-B);

    public static IdleCurrency operator +(IdleCurrency A, double _input) => A + new IdleCurrency(_input, A.curreny_type);
    public static IdleCurrency operator -(IdleCurrency A, double _input) => A + -(new IdleCurrency(_input, A.curreny_type));
    public static IdleCurrency operator *(IdleCurrency A, double _input)
    {
        var _temp = new IdleCurrency(A);

        if (double.IsInfinity(_input))
        {
            Debug.LogError("_input is infinity");
            return _temp;
        }
        if (_input * NUM_MAX > NUM_TYPE.MaxValue)
        {
            Debug.LogError("_input is so huge");
            return _temp;
        }

        var _temp_key_array = _temp.values.Keys.ToArray();
        foreach (int _idx in _temp_key_array)
        {
            _temp.values[_idx] = Mathf.RoundToInt(_temp.values[_idx] * (float)_input);
        }
        _temp.RefreshCurrencyNumList();

        return _temp;
    }
    public static IdleCurrency operator /(IdleCurrency A, double _input)
    {
        if (_input == 0)
        {
            throw new System.ArgumentException("It can't be divided into zero");
        }
        var _temp = new IdleCurrency(A);

        var _temp_key_array = _temp.values.Keys.ToArray();
        foreach (int _idx in _temp_key_array)
        {
            _temp.values[_idx] = Mathf.RoundToInt(_temp.values[_idx] / (float)_input);
        }

        _temp.RefreshCurrencyNumList();

        return _temp;
    }

    public static bool operator >(IdleCurrency A, IdleCurrency B)
    {
        if (A.curreny_type != B.curreny_type)
        {
            throw new System.ArgumentException("다른 타입끼리 비교를 시도함 " + A.curreny_type + " != " + B.curreny_type);
        }

        if (A.unit == B.unit)
        {
            if (A.isNegative & B.isNegative)
                return (A.last_num != B.last_num) ? A.last_num < B.last_num : A.second_last_num < B.second_last_num;
            else if (!A.isNegative & !B.isNegative)
                return (A.last_num != B.last_num) ? A.last_num > B.last_num : A.second_last_num > B.second_last_num;
            else
            {
                if (A.isNegative) return false;
                else return false;
            }
        }
        else
        {
            if (A.isNegative & B.isNegative)
                return A.unit < B.unit;
            else if (!A.isNegative & !B.isNegative)
                return A.unit > B.unit;
            else
            {
                if (A.isNegative) return false;
                else return true;
            }
        }
    }
    public static bool operator <(IdleCurrency A, IdleCurrency B) => B > A;
    public static bool operator >=(IdleCurrency A, IdleCurrency B)
    {
        if (A.curreny_type != B.curreny_type)
        {
            throw new System.ArgumentException("다른 타입끼리 연산을 시도함");
        }

        if (A == B)
            return true;
        else
            return A > B;
    }
    public static bool operator <=(IdleCurrency A, IdleCurrency B) => B >= A;
    public static bool operator >(IdleCurrency A, double _input) => A > new IdleCurrency(_input, A.curreny_type);
    public static bool operator <(IdleCurrency A, double _input) => new IdleCurrency(_input, A.curreny_type) > A;
    public static bool operator >=(IdleCurrency A, double _input) => A >= new IdleCurrency(_input, A.curreny_type);
    public static bool operator <=(IdleCurrency A, double _input) => new IdleCurrency(_input, A.curreny_type) >= A;
    public static bool operator ==(IdleCurrency A, IdleCurrency B)
    {
        if (A is null || B is null)
        {
            return (object)A == (object)B; // TODO : 게임상 null값끼리 비교가 의미없으면 그냥 false 리턴
        }

        if (A.curreny_type != B.curreny_type)
        {
            throw new System.ArgumentException("다른 타입끼리 비교를 시도함 " + A.curreny_type + " != " + B.curreny_type);
        }

        // 최근 등급의 값만 비교
        return A.last_num == B.last_num && A.second_last_num == B.second_last_num && A.unit == B.unit && A.isNegative == B.isNegative;
    }
    public static bool operator !=(IdleCurrency A, IdleCurrency B) => !(A == B);
#endregion
    /// <summary>
    /// 객체 자체가 같은 것인지 확인
    /// </summary>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    /// <summary>
    /// IdleCurrency 간 값들만 비교(operator ==) 
    /// </summary>
    /// <returns></returns>
    public bool ValueEquals(IdleCurrency obj)
    {
        return this == obj;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        string unit_string = "";
        if (unit > 0)
        {
            var _unit = unit - 1;
            int loop_count = 0;
            while (_unit >= 0)
            {
                if (_unit / 26 > 0)
                {
                    unit_string += (char)('a' + _unit % 26);
                    _unit /= 26;
                }
                else
                {
                    unit_string += (char)('a' + _unit - ((unit - 1) / 26 > 0 ? 1 : 0));
                    _unit = -1;
                }
                loop_count++;
                if (loop_count >= MAX_LOOP_COUNT)
                {
                    Debug.LogError("최대 루프 횟수 도달\nthis.killo_count : " + this.unit + "\nthis.type : " + this.curreny_type);
                    break;
                }
            }
            var reverse_array = unit_string.ToCharArray();
            System.Array.Reverse(reverse_array);
            unit_string = new string(reverse_array);
        }
        return (isNegative ? "-" : "") + last_num + "." + (second_last_num / 10).ToString("00") + unit_string;
    }
}
