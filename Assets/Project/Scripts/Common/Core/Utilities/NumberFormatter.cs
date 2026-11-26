using System;
using System.Globalization;

/// <summary>
/// 게임에서 사용하는 숫자 표시 형식을 공통으로 관리
/// 숫자 데이터를 변경하지 않고 UI에 표시할 문자열만 반환
/// </summary>
public static class NumberFormatter 
{
    /// <summary>
    /// 숫자 표시 형식에 사용할 문화권 고정
    /// PC의 지역 설정에 따라 소수점과 천 단위 구분 기호가 달라지는 것 방지
    /// </summary>
    private static readonly CultureInfo _formatCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// 일반 정수를 천 단위 구분 기호와 함께 표시
    /// 1234 → 1,234
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatInteger(int value)
    {
        return value.ToString("N0", _formatCulture);
    }

    /// <summary>
    /// 10,000 이상인 숫자를 만 단위로 축약
    /// 10000 → 1만, 12500 → 1.25만
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatCompact(double value)
    {
        double absValue = Math.Abs(value);

        if (absValue >= 100000000)
        {
            return (value / 100000000.0).ToString("0.##", _formatCulture) + "억";
        }

        if (absValue >= 10000)
        {
            return (value / 10000.0).ToString("0.##", _formatCulture) + "만";
        }

        return value.ToString("N0", _formatCulture);
    }

    /// <summary>
    /// 지정한 소수 자릿수까지 표시, 기본값은 소수 첫째자리까지
    /// 1.25 → 1.3, 0.5f → 0.5
    /// </summary>
    /// <param name="value"></param>
    /// <param name="decimalPlaces"></param>
    /// <returns></returns>
    public static string FormatFixed(float value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0 || decimalPlaces > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        }

        string format = "F" + decimalPlaces;

        return value.ToString(format, _formatCulture);
    }

    /// <summary>
    /// 불필요한 소수점 이하 0을 제거
    /// 2.50f → 2.5, 1.0f → 1
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FormatTrimmed(float value)
    {
        return value.ToString("0.####", _formatCulture);
    }

    /// <summary>
    /// 0~1 사이의 비율을 백분율로 변환해 표시, 기본적으로 소수점 없이 표시
    /// 0.15f → 15%, 0.156f, 1 → 15.6%
    /// </summary>
    /// <param name="ratio"></param>
    /// <param name="decimalPlaces"></param>
    /// <returns></returns>
    public static string FormatPercent(float ratio, int decimalPlaces = 0)
    {
        return FormatFixed(ratio * 100f, decimalPlaces) + "%";
    }
}
