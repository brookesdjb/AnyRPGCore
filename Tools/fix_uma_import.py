#!/usr/bin/env python3
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]

MESH_HIDE = ROOT / "Assets/UMA/Core/StandardAssets/UMA/Scripts/MeshHideAsset.cs"
SSS_UTILS = ROOT / "Assets/UMA/Content/SkinShaders/Shader/SSS_Utils.cginc"


def update_mesh_hide(path: pathlib.Path) -> bool:
    if not path.exists():
        print(f"warn: missing {path}")
        return False
    text = path.read_text(encoding="utf-8")
    pattern = r"\[SerializeField\]\s*\n(\s*public\s+SlotDataAsset\s+asset)"
    new_text, count = re.subn(pattern, r"\1", text, count=1)
    if count:
        path.write_text(new_text, encoding="utf-8")
        print(f"updated: {path}")
        return True
    print(f"unchanged: {path}")
    return False


def update_sss_utils(path: pathlib.Path) -> bool:
    if not path.exists():
        print(f"warn: missing {path}")
        return False
    text = path.read_text(encoding="utf-8")
    replacements = {
        r"(?<!SSS_)RoughnessToPerceptualRoughness": "SSS_RoughnessToPerceptualRoughness",
        r"(?<!SSS_)RoughnessToPerceptualSmoothness": "SSS_RoughnessToPerceptualSmoothness",
        r"(?<!SSS_)PerceptualSmoothnessToRoughness": "SSS_PerceptualSmoothnessToRoughness",
        r"(?<!SSS_)PerceptualSmoothnessToPerceptualRoughness": "SSS_PerceptualSmoothnessToPerceptualRoughness",
        r"(?<!SSS_)PerceptualRoughnessToPerceptualSmoothness": "SSS_PerceptualRoughnessToPerceptualSmoothness",
    }
    new_text = text
    for pattern, repl in replacements.items():
        new_text = re.sub(pattern, repl, new_text)

    if new_text != text:
        path.write_text(new_text, encoding="utf-8")
        print(f"updated: {path}")
        return True

    print(f"unchanged: {path}")
    return False


def main() -> int:
    changed = False
    changed |= update_mesh_hide(MESH_HIDE)
    changed |= update_sss_utils(SSS_UTILS)
    return 0


if __name__ == "__main__":
    sys.exit(main())
