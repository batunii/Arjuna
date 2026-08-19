# -*- coding: utf-8 -*-
"""ESQ extraction, all forms. Answer = Y/N written in the Key column; '+'/'(R)' = unanswered.
Favourable = agree(Y) on positively keyed items, disagree(N) on reverse-keyed items.
Validates against the recorded 16-form totals (A 71/80, B 46/80, C 45/64) before adding
the 2026-08-11/12 forms (P51, P90).
P84/P86: see Dissertation/authored/provenance/README-p90-blocka-provenance.md."""
import sys, zipfile, re, glob, collections
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')

# Resolve relative to this file so the script runs on any machine and from any working directory.
DIR = str((Path(__file__).resolve().parents[2] / 'Dissertation'))
if not Path(DIR).is_dir():
    raise SystemExit(f'questionnaire directory not found: {DIR}')

# True item keys, from the instrument (post-study-questionnaire.md / testing-strategy-v2 s7).
# '+' = agreement favourable to the filter; 'R' = disagreement favourable.
KEYS = {
    'A1': '+', 'A2': 'R', 'A3': '+', 'A4': 'R', 'A5': '+',
    'B1': 'R', 'B2': '+', 'B3': 'R', 'B4': '+', 'B5': 'R',
    'C1': '+', 'C2': 'R', 'C3': '+', 'C4': 'R', 'C5': '+',
}
IDS = list(KEYS)

def doc_tokens(path):
    z = zipfile.ZipFile(path)
    xml = z.read('word/document.xml').decode('utf-8')
    txt = re.sub(r'<[^>]+>', '|', xml)
    return [t.strip() for t in txt.split('|') if t.strip()]

def answers(path):
    toks = doc_tokens(path)
    ans = {}
    for idx, t in enumerate(toks):
        if t in IDS and t not in ans:
            # find the answer among the next tokens, stopping at the next item id / section
            for u in toks[idx + 1: idx + 12]:
                if u in ('Y', 'N', 'y', 'n'):
                    ans[t] = u.upper()
                    break
                if u in IDS or u.startswith('Section'):
                    break
    return ans

# Participant IDs only. P15 and P16 are one participant on a single combined form.
# The 16 forms used for the validation total, then the four added 2026-08-11/13.
OLD = ['2', '3', '4', '9', '10', '13', '15-16', '17', '20', '25', '26', '27',
       '30', '33', '41', '43']
NEW = ['51', '90', '1', '6']

def form_path(pid):
    """Locate a participant's questionnaire by ID, whatever suffix the filename carries."""
    for pattern in (rf'\post-study-questionnaire-{pid}.docx',
                    rf'\post-study-questionnaire-{pid}-*.docx'):
        hits = sorted(glob.glob(DIR + pattern))
        if hits:
            return hits[0]
    raise SystemExit(f'no questionnaire form found for participant {pid} in {DIR}')

# The per-participant .docx forms were consolidated into a single markdown record and removed
# from the repository. That record is authoritative; the .docx reader below stays as a fallback
# for anyone working from the original forms.
COMBINED = Path(DIR) / 'post-study-questionnaire-responses-combined.md'
# The sheet filed as "15-16" is one participant, recorded under 16 in the combined file.
COMBINED_ALIASES = {'15-16': '16'}

def load_combined():
    if not COMBINED.is_file():
        return None
    rows = collections.defaultdict(dict)
    for line in COMBINED.read_text(encoding='utf-8').splitlines():
        cells = [c.strip() for c in line.split('|')[1:-1]]
        if len(cells) != 4:
            continue
        pid, item, _question, a = cells
        if item in KEYS and a in ('Yes', 'No'):
            rows[pid][item] = 'Y' if a == 'Yes' else 'N'
    return rows or None

COMBINED_ANSWERS = load_combined()

def get_answers(pid):
    """Answers for one participant, from the combined record or the original .docx."""
    if COMBINED_ANSWERS is not None:
        key = COMBINED_ALIASES.get(pid, pid)
        if key in COMBINED_ANSWERS:
            return COMBINED_ANSWERS[key]
    return answers(form_path(pid))

def favourable(ans):
    fav = {}
    for item, a in ans.items():
        fav[item] = (a == 'Y') == (KEYS[item] == '+')
    return fav

def tally(names, label):
    per_item_fav = collections.Counter()
    per_item_n = collections.Counter()
    sect_fav = collections.Counter()
    sect_n = collections.Counter()
    per_pid = {}
    for nm in names:
        ans = get_answers(nm)
        fav = favourable(ans)
        per_pid[nm] = ans
        for item, isfav in fav.items():
            per_item_n[item] += 1
            per_item_fav[item] += isfav
            sect_n[item[0]] += 1
            sect_fav[item[0]] += isfav
    print(f'== {label}: {len(names)} forms ==')
    for s in 'ABC':
        print(f'  Section {s}: {sect_fav[s]}/{sect_n[s]}')
    for item in IDS:
        if per_item_n[item]:
            print(f'    {item}: {per_item_fav[item]}/{per_item_n[item]}')
    return per_pid

old = tally(OLD, 'validation set (recorded: A 71/80, B 46/80, C 45/64)')
print()
new = tally(NEW, 'new forms')
print()
allp = tally(OLD + NEW, 'FULL SET n=20')
print('\nper-form answers (new):')
for nm, ans in new.items():
    print(' ', nm, ans)
# strong dissenter check (P25-like: A favourable count per pid)
print('\nA-block favourable per respondent:')
for nm in OLD + NEW:
    fav = favourable(get_answers(nm))
    a = sum(v for k, v in fav.items() if k.startswith('A'))
    an = sum(1 for k in fav if k.startswith('A'))
    print(f'  {nm}: {a}/{an}')
