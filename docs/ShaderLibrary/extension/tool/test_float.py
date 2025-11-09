#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
浮点数内存布局解析工具
用于解析和显示32位float和16位half的内存布局
"""

import struct
import sys
import math
import argparse

# IEEE 754 32-bit float constants
FLOAT_SIGN_BITS = 1
FLOAT_EXP_BITS = 8
FLOAT_MANT_BITS = 23
FLOAT_EXP_BIAS = 127
FLOAT_EXP_MAX = (2**FLOAT_EXP_BITS - 1)

# IEEE 754 16-bit half constants
HALF_SIGN_BITS = 1
HALF_EXP_BITS = 5
HALF_MANT_BITS = 10
HALF_EXP_BIAS = 15
HALF_EXP_MAX = (2**HALF_EXP_BITS - 1)


def format_binary(binary_str, group_size=4, separator=' '):
    """Formats a binary string into groups with separators."""
    return separator.join([binary_str[i:i+group_size] for i in range(0, len(binary_str), group_size)])


def format_layout(s, e_bits, m_bits):
    """Formats S|E|M layout in one line."""
    e_formatted = format_binary(e_bits)
    m_formatted = format_binary(m_bits)
    return f"{s} | {e_formatted} | {m_formatted}"


def parse_float32(value):
    """Parses a 32-bit float and returns its components."""
    if isinstance(value, str) and value.startswith('0x'):
        # Interpret as hex bit pattern
        int_val = int(value, 16)
        float_val = struct.unpack('>f', struct.pack('>I', int_val))[0]
    else:
        float_val = float(value)
        int_val = struct.unpack('>I', struct.pack('>f', float_val))[0]

    binary_repr = bin(int_val)[2:].zfill(32)

    s = int(binary_repr[0])
    e_bits = binary_repr[FLOAT_SIGN_BITS : FLOAT_SIGN_BITS + FLOAT_EXP_BITS]
    m_bits = binary_repr[FLOAT_SIGN_BITS + FLOAT_EXP_BITS :]

    e_val = int(e_bits, 2)
    m_val = int(m_bits, 2)

    # Determine type and calculate decimal value
    value_type = "正规数"
    decimal_val = float_val  # Use struct's conversion for accuracy

    if e_val == 0 and m_val == 0:
        value_type = "零"
        decimal_val = 0.0 if s == 0 else -0.0
    elif e_val == FLOAT_EXP_MAX and m_val == 0:
        value_type = "无穷大"
        decimal_val = float('inf') if s == 0 else float('-inf')
    elif e_val == FLOAT_EXP_MAX and m_val != 0:
        value_type = "NaN"
        decimal_val = float('nan')
    elif e_val == 0 and m_val != 0:
        value_type = "非正规数"
        # Denormalized: (-1)^S * 0.M * 2^(-126)
        mantissa_decimal = m_val / (2**FLOAT_MANT_BITS)
        decimal_val = ((-1)**s) * mantissa_decimal * (2**(-FLOAT_EXP_BIAS + 1))

    actual_exp = e_val - FLOAT_EXP_BIAS if e_val != 0 and e_val != FLOAT_EXP_MAX else None
    
    return {
        "original_input": value,
        "decimal_val": decimal_val,
        "s": s,
        "e_bits": e_bits,
        "m_bits": m_bits,
        "e_val": e_val,
        "m_val": m_val,
        "actual_exp": actual_exp,
        "full_binary": binary_repr,
        "hex_repr": hex(int_val),
        "type": value_type
    }


def parse_float16(value):
    """Parses a 16-bit half float and returns its components."""
    if isinstance(value, str) and value.startswith('0x'):
        # For half, we need to handle hex input differently
        # If it's a 16-bit hex pattern, use it directly
        if len(value) <= 6:  # 0xXXXX format
            int_val = int(value, 16)
            try:
                half_val = struct.unpack('>e', struct.pack('>H', int_val))[0]
            except:
                # If conversion fails, try to interpret as float32 hex and convert
                int_val_32 = int(value, 16) << 16  # Pad to 32 bits
                float_val = struct.unpack('>f', struct.pack('>I', int_val_32))[0]
                half_val = float_val
        else:
            # 32-bit hex pattern, convert to float then to half
            int_val_32 = int(value, 16)
            float_val = struct.unpack('>f', struct.pack('>I', int_val_32))[0]
            half_val = float_val
            int_val = struct.unpack('>H', struct.pack('>e', half_val))[0]
    else:
        half_val = float(value)
        int_val = struct.unpack('>H', struct.pack('>e', half_val))[0]

    binary_repr = bin(int_val)[2:].zfill(16)

    s = int(binary_repr[0])
    e_bits = binary_repr[HALF_SIGN_BITS : HALF_SIGN_BITS + HALF_EXP_BITS]
    m_bits = binary_repr[HALF_SIGN_BITS + HALF_EXP_BITS :]

    e_val = int(e_bits, 2)
    m_val = int(m_bits, 2)

    # Determine type and calculate decimal value
    value_type = "正规数"
    decimal_val = half_val  # Use struct's conversion for accuracy

    if e_val == 0 and m_val == 0:
        value_type = "零"
        decimal_val = 0.0 if s == 0 else -0.0
    elif e_val == HALF_EXP_MAX and m_val == 0:
        value_type = "无穷大"
        decimal_val = float('inf') if s == 0 else float('-inf')
    elif e_val == HALF_EXP_MAX and m_val != 0:
        value_type = "NaN"
        decimal_val = float('nan')
    elif e_val == 0 and m_val != 0:
        value_type = "非正规数"
        # Denormalized: (-1)^S * 0.M * 2^(-14)
        mantissa_decimal = m_val / (2**HALF_MANT_BITS)
        decimal_val = ((-1)**s) * mantissa_decimal * (2**(-HALF_EXP_BIAS + 1))

    actual_exp = e_val - HALF_EXP_BIAS if e_val != 0 and e_val != HALF_EXP_MAX else None

    return {
        "original_input": value,
        "decimal_val": decimal_val,
        "s": s,
        "e_bits": e_bits,
        "m_bits": m_bits,
        "e_val": e_val,
        "m_val": m_val,
        "actual_exp": actual_exp,
        "full_binary": binary_repr,
        "hex_repr": hex(int_val),
        "type": value_type
    }


def calculate_mantissa_decimal(s, e_val, m_val, mantissa_bits, is_half=False):
    """Calculate the decimal representation of mantissa (1.M for normalized, 0.M for denormalized)."""
    if e_val == 0:
        # Denormalized: 0.M
        return m_val / (2**mantissa_bits)
    else:
        # Normalized: 1.M
        return 1.0 + m_val / (2**mantissa_bits)


def calculate_absolute_precision(actual_exp, mantissa_bits):
    """Calculate absolute precision for a given exponent."""
    if actual_exp is None:
        return None
    return 2**(actual_exp - mantissa_bits)


def calculate_next_float(value, is_half=False):
    """Calculate the next representable float value."""
    try:
        if is_half:
            # For half, convert to float, increment, then convert back
            int_val = struct.unpack('>H', struct.pack('>e', value))[0]
            if int_val == 0x7C00:  # Positive infinity
                return float('inf')
            if int_val == 0xFC00:  # Negative infinity
                return float('-inf')
            if int_val >= 0x7C00:  # NaN or infinity
                return value
            next_int = int_val + 1
            if next_int > 0x7C00:  # Would overflow to infinity
                return float('inf') if value > 0 else float('-inf')
            return struct.unpack('>e', struct.pack('>H', next_int))[0]
        else:
            # For float
            int_val = struct.unpack('>I', struct.pack('>f', value))[0]
            if int_val == 0x7F800000:  # Positive infinity
                return float('inf')
            if int_val == 0xFF800000:  # Negative infinity
                return float('-inf')
            if int_val >= 0x7F800000:  # NaN or infinity
                return value
            next_int = int_val + 1
            if next_int > 0x7F800000:  # Would overflow to infinity
                return float('inf') if value > 0 else float('-inf')
            return struct.unpack('>f', struct.pack('>I', next_int))[0]
    except (OverflowError, struct.error):
        return None


def print_float_info(title, data, is_half=False):
    """Prints formatted float information with header."""
    print(f"{title}:")
    print(f"  符号位 | 指数位 | 尾数位")
    
    layout_str = format_layout(data['s'], data['e_bits'], data['m_bits'])
    print(f"  {layout_str}")
    
    bit_width = 16 if is_half else 32
    print(f"  完整{bit_width}位: {format_binary(data['full_binary'])}")
    print(f"  十六进制: {data['hex_repr']}")
    
    type_info = f"  类型: {data['type']}"
    if data['actual_exp'] is not None:
        type_info += f" (指数: {data['e_val']}, 实际指数: {data['actual_exp']})"
    else:
        type_info += f" (指数: {data['e_val']})"
    print(type_info)
    
    # Calculate and display additional information for normalized numbers
    if data['type'] == "正规数" and data['actual_exp'] is not None:
        mantissa_bits = HALF_MANT_BITS if is_half else FLOAT_MANT_BITS
        mantissa_decimal = calculate_mantissa_decimal(
            data['s'], data['e_val'], data['m_val'], mantissa_bits, is_half
        )
        absolute_precision = calculate_absolute_precision(data['actual_exp'], mantissa_bits)
        next_float = calculate_next_float(data['decimal_val'], is_half)
        
        print(f"  尾数: {mantissa_decimal:.6f}")
        print(f"  指数: {data['actual_exp']}")
        if absolute_precision is not None:
            # Format absolute precision appropriately
            if absolute_precision >= 1:
                print(f"  绝对精度: {absolute_precision:.0f}")
            elif absolute_precision >= 0.001:
                print(f"  绝对精度: {absolute_precision:.6f}")
            else:
                print(f"  绝对精度: {absolute_precision:.6e}")
        if next_float is not None:
            if math.isinf(next_float):
                print(f"  下一个浮点数: {'inf' if next_float > 0 else '-inf'}")
            else:
                # Format next float appropriately
                # For large integers (like 10000001), show as integer
                if abs(next_float) >= 1 and abs(next_float) < 1e15:
                    # Check if it's effectively an integer (difference from int is very small)
                    int_val = int(next_float)
                    if abs(next_float - int_val) < 1e-6:
                        print(f"  下一个浮点数: {int_val}")
                    elif abs(next_float) >= 1e6:
                        print(f"  下一个浮点数: {next_float:.6e}")
                    else:
                        # Show with enough precision to see the difference
                        print(f"  下一个浮点数: {next_float:.10f}".rstrip('0').rstrip('.'))
                elif abs(next_float) < 1e-3 and next_float != 0:
                    print(f"  下一个浮点数: {next_float:.6e}")
                else:
                    print(f"  下一个浮点数: {next_float:.10f}".rstrip('0').rstrip('.'))


def format_decimal_value(val):
    """Formats decimal value for table display."""
    if math.isnan(val):
        return "nan"
    elif math.isinf(val):
        return "inf" if val > 0 else "-inf"
    elif val == 0.0:
        # Check sign bit by comparing with -0.0
        if str(val) == "-0.0" or (val == 0.0 and math.copysign(1, val) < 0):
            return "-0.0"
        return "0.0"
    else:
        # Use scientific notation for very large or very small numbers
        if abs(val) >= 1e10 or (abs(val) < 1e-3 and val != 0):
            return f"{val:.6e}"
        else:
            return f"{val:.10f}".rstrip('0').rstrip('.')


def print_special_values_table():
    """Prints a table of all special float values."""
    special_values = [
        ("正零", "0x00000000"),
        ("负零", "0x80000000"),
        ("正无穷", "0x7F800000"),
        ("负无穷", "0xFF800000"),
        ("Quiet NaN", "0x7FC00000"),
        ("Signaling NaN", "0x7F800001"),
        ("最大值", "0x7F7FFFFF"),
        ("最小正规数", "0x00800000"),
        ("最小非正规数", "0x00000001"),
    ]
    
    # Parse all values
    rows = []
    for name, hex_val in special_values:
        float32_data = parse_float32(hex_val)
        
        # Try to get half representation
        try:
            float_val = float32_data['decimal_val']
            if math.isnan(float_val) or math.isinf(float_val):
                # For NaN/Inf, try to use the float32 hex pattern directly
                # Convert float32 hex to half if possible
                half_data = None
                half_layout = "N/A"
            else:
                half_data = parse_float16(float_val)
                half_layout = format_layout(half_data['s'], half_data['e_bits'], half_data['m_bits'])
        except (OverflowError, ValueError, struct.error):
            half_layout = "N/A"
            half_data = None
        
        float32_layout = format_layout(float32_data['s'], float32_data['e_bits'], float32_data['m_bits'])
        
        # For zero values, format based on sign bit
        if float32_data['type'] == "零":
            decimal_str = "-0.0" if float32_data['s'] == 1 else "0.0"
        else:
            decimal_str = format_decimal_value(float32_data['decimal_val'])
        
        # Format hex consistently (always 10 characters: 0x + 8 hex digits)
        hex_str = float32_data['hex_repr']
        if hex_str.startswith('0x'):
            hex_val = int(hex_str, 16)
            hex_str = f"0x{hex_val:08X}"
        
        rows.append({
            "name": name,
            "decimal": decimal_str,
            "float32_layout": float32_layout,
            "half_layout": half_layout,
            "hex": hex_str,
            "type": float32_data['type']
        })
    
    # Define header labels
    header_labels = {
        "name": "名称",
        "decimal": "十进制值",
        "float32_layout": "32位布局 (S|E|M)",
        "half_layout": "16位布局 (S|E|M)",
        "hex": "十六进制 (32位)",
        "type": "类型"
    }
    
    # Calculate column widths (consider both header and data)
    col_widths = {
        "name": max(len(row["name"]) for row in rows),
        "decimal": max(len(row["decimal"]) for row in rows),
        "float32_layout": max(len(row["float32_layout"]) for row in rows),
        "half_layout": max(len(row["half_layout"]) for row in rows),
        "hex": max(len(row["hex"]) for row in rows),
        "type": max(len(row["type"]) for row in rows),
    }
    
    # Ensure minimum widths based on header length
    col_widths["name"] = max(col_widths["name"], len(header_labels["name"]))
    col_widths["decimal"] = max(col_widths["decimal"], len(header_labels["decimal"]))
    col_widths["float32_layout"] = max(col_widths["float32_layout"], len(header_labels["float32_layout"]))
    col_widths["half_layout"] = max(col_widths["half_layout"], len(header_labels["half_layout"]))
    col_widths["hex"] = max(col_widths["hex"], len(header_labels["hex"]))
    col_widths["type"] = max(col_widths["type"], len(header_labels["type"]))
    
    # Print header
    print("特殊值内存布局表：\n")
    
    # Build header using consistent format
    header_parts = [
        f"{header_labels['name']:<{col_widths['name']}}",
        f"{header_labels['decimal']:<{col_widths['decimal']}}",
        f"{header_labels['float32_layout']:<{col_widths['float32_layout']}}",
        f"{header_labels['half_layout']:<{col_widths['half_layout']}}",
        f"{header_labels['hex']:<{col_widths['hex']}}",
        f"{header_labels['type']:<{col_widths['type']}}"
    ]
    header = " | ".join(header_parts)
    print(header)
    
    # Print separator (aligned with columns, matching the " | " format)
    separator_parts = [
        '-' * col_widths['name'],
        '-' * col_widths['decimal'],
        '-' * col_widths['float32_layout'],
        '-' * col_widths['half_layout'],
        '-' * col_widths['hex'],
        '-' * col_widths['type']
    ]
    separator = " | ".join(separator_parts)
    print(separator)
    
    # Print rows using consistent format
    for row in rows:
        row_parts = [
            f"{row['name']:<{col_widths['name']}}",
            f"{row['decimal']:<{col_widths['decimal']}}",
            f"{row['float32_layout']:<{col_widths['float32_layout']}}",
            f"{row['half_layout']:<{col_widths['half_layout']}}",
            f"{row['hex']:<{col_widths['hex']}}",
            f"{row['type']:<{col_widths['type']}}"
        ]
        line = " | ".join(row_parts)
        print(line)


def main():
    parser = argparse.ArgumentParser(
        description="解析浮点数内存布局。",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例:
  python3 test_float.py 3.14
  python3 test_float.py 0x40490FDA
  python3 test_float.py --help
        """
    )
    parser.add_argument(
        "float_value",
        nargs='?',
        help="要解析的浮点数（十进制或十六进制，如 '3.14' 或 '0x40490FDB'）。"
    )
    parser.add_argument(
        "--special",
        action="store_true",
        dest="show_special",
        help="显示所有特殊值的内存布局表。"
    )
    
    args = parser.parse_args()
    
    # Handle --special flag or no argument
    if args.show_special or not args.float_value:
        print_special_values_table()
        return
    
    value = args.float_value
    
    print(f"输入值: {value}")
    
    try:
        float32_data = parse_float32(value)
        print(f"解析值: {format_decimal_value(float32_data['decimal_val'])}")
        print()
        print_float_info("32位 float", float32_data)
        
        print()
        
        try:
            half_data = parse_float16(value)
            print_float_info("16位 half", half_data, is_half=True)
        except OverflowError:
            print("16位 half: 无法表示此值 (溢出或精度不足)")
        except Exception as e:
            print(f"16位 half: 处理时发生错误 - {e}")
    except ValueError as e:
        print(f"错误: 无效的输入值 - {e}")
        sys.exit(1)
    except Exception as e:
        print(f"错误: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()

