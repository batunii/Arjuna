"""Verify phrases in a PDF text layer, tolerant of pdflatex kerning splits.

Usage: python notes/_pdfverify.py <pdf>
Compares against a fixed checklist; prints FOUND/absent per phrase.
Whitespace- and parenthesis-insensitive: pdflatex splits strings mid-word for
kerning, so naive phrase search gives false negatives.
"""
import re
import sys
import zlib

PRESENT = {
    "analysed-N disclaimer": "reduce the analysed sets below it",
    "nineteen and seventeen analysed": "nineteen and seventeen analysed",
    "counterbalanced threats row": "neither block is systematically second",
    "nominal timeline clause": "block order shown below is nominal",
    "control: Leave-one-out heading": "Leave-one-out robustness",
    "control: session validator": "session validator",
    "control: no measured loss": "no measured loss",
}
ABSENT = {
    "GONE: B is second for every": "B is second for every",
    "GONE: Fixed A->B limitation": "block order is a limitation",
    "GONE: twenty-two": "twenty-two",
}


def text_layer(path):
    data = open(path, "rb").read()
    chunks = []
    for m in re.finditer(rb"stream\r?\n(.*?)endstream", data, re.S):
        try:
            chunks.append(zlib.decompress(m.group(1)))
        except Exception:
            pass
    raw = b" ".join(chunks).decode("latin-1")
    # keep only plausible text characters, drop PDF operator punctuation that
    # kerning uses to break words apart
    keep = re.sub(r"[^A-Za-z0-9 ,.;:%*/+='\"-]", " ", raw)
    return re.sub(r"\s+", "", keep).lower()


def main(argv):
    pdf = argv[0] if argv else "thesis.pdf"
    hay = text_layer(pdf)
    print("normalised text-layer chars: %d" % len(hay))
    ok = True
    print("\nmust be PRESENT:")
    for label, phrase in PRESENT.items():
        needle = re.sub(r"\s+", "", phrase).lower()
        n = hay.count(needle)
        print("  %-34s %-7s (%d)" % (label, "FOUND" if n else "ABSENT", n))
        if not n:
            ok = False
    print("\nmust be ABSENT:")
    for label, phrase in ABSENT.items():
        needle = re.sub(r"\s+", "", phrase).lower()
        n = hay.count(needle)
        print("  %-34s %-7s (%d)" % (label, "clean" if n == 0 else "HIT", n))
        if n:
            ok = False
    print("\nOVERALL:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
