#!/usr/bin/env python3
"""
generate_testdata.py: generate YAML test files for benchmarks.

This script creates the following files under the TestData folder:
  - small.yaml: flat mapping (default 100 entries)
  - large.yaml: flat mapping (default 10_000 entries)
  - nested.yaml: nested mappings (default depth 5, width 5)
  - arrays.yaml: sequence of objects (default 100 items, 5 properties each)
  - complex.yaml: example with multi-line strings, aliases, and anchors

Run without arguments to use defaults, or override sizes, depths, and output directory.
"""
import argparse
import os


def generate_yaml_mapping(count: int) -> str:
    lines = []
    for i in range(count):
        # simple key-value mapping
        lines.append(f"key{i}: \"value{i}\"")
    return "\n".join(lines)

def generate_nested_yaml(depth: int, width: int) -> str:
    lines: list[str] = []
    for level in range(depth):
        indent = "  " * level
        for i in range(width):
            lines.append(f"{indent}key{level}_{i}: \"value{level}_{i}\"")
        if level + 1 < depth:
            lines.append(f"{indent}nested{level}:")
    return "\n".join(lines)

def generate_arrays_yaml(count: int, props: int) -> str:
    lines: list[str] = []
    lines.append("items:")
    for i in range(count):
        lines.append(f"  - id: {i}")
        for j in range(props):
            # Indent properties under the sequence item by two spaces beyond the dash
            lines.append(f"    prop{j}: \"value{j}_{i}\"")
    return "\n".join(lines)

def generate_complex_yaml() -> str:
    return """\
default:
  host: default.host
  port: 1234

development:
  host: default.host
  port: 1234
  database: dev_db
  description: |
    This is a multi-line
    description with YAML
    block scalar.

production:
  host: default.host
  port: 1234
  database: prod_db
  description: >
    Production description
    with folded newlines
    and special characters: !@#$%^&*()
"""


def write_file(path: str, content: str) -> None:
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
        f.write("\n")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Generate YAML test data files for VYaml.Configuration benchmarks."
    )
    parser.add_argument(
        "--small-size", type=int, default=100,
        help="Number of entries in small.yaml (default: 100)"
    )
    parser.add_argument(
        "--large-size", type=int, default=1000,
        help="Number of entries in large.yaml (default: 1000)"
    )
    parser.add_argument(
        "--nested-depth", type=int, default=4,
        help="Depth of nested mapping for nested.yaml (default: 4)"
    )
    parser.add_argument(
        "--nested-width", type=int, default=10,
        help="Number of keys per level for nested.yaml (default: 10)"
    )
    parser.add_argument(
        "--array-size", type=int, default=500,
        help="Number of objects in arrays.yaml (default: 500)"
    )
    parser.add_argument(
        "--array-props", type=int, default=5,
        help="Number of properties per object in arrays.yaml (default: 5)"
    )
    parser.add_argument(
        "--output-dir", default=os.path.dirname(__file__),
        help="Directory to output test files (default: TestData folder)"
    )
    args = parser.parse_args()

    os.makedirs(args.output_dir, exist_ok=True)

    # Flat mapping files
    small_yaml = generate_yaml_mapping(args.small_size)
    large_yaml = generate_yaml_mapping(args.large_size)
    write_file(os.path.join(args.output_dir, "small.yaml"), small_yaml)
    write_file(os.path.join(args.output_dir, "large.yaml"), large_yaml)

    # Nested mapping file
    nested_yaml = generate_nested_yaml(args.nested_depth, args.nested_width)
    write_file(os.path.join(args.output_dir, "nested.yaml"), nested_yaml)

    # Arrays of objects file
    arrays_yaml = generate_arrays_yaml(args.array_size, args.array_props)
    write_file(os.path.join(args.output_dir, "arrays.yaml"), arrays_yaml)

    # Complex features file
    complex_yaml = generate_complex_yaml()
    write_file(os.path.join(args.output_dir, "complex.yaml"), complex_yaml)

    print(f"Generated test files in '{args.output_dir}':")
    print(f"  small.yaml ({args.small_size} entries)")
    print(f"  large.yaml ({args.large_size} entries)")
    print(f"  nested.yaml (depth={args.nested_depth}, width={args.nested_width})")
    print(f"  arrays.yaml (count={args.array_size}, props={args.array_props})")
    print("  complex.yaml (static complex features)")


if __name__ == "__main__":
    main()