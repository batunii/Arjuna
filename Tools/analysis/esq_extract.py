# -*- coding: utf-8 -*-
"""ESQ extraction, all forms. Answer = Y/N written in the Key column; '+'/'(R)' = unanswered.
Favourable = agree(Y) on positively keyed items, disagree(N) on reverse-keyed items.
Validates against the recorded 16-form totals (A 71/80, B 46/80, C 45/64) before adding
the 2026-08-11/12 forms (P51, P90). P84 and P86 were dropped 2026-08-12: their raw session
files and questionnaire forms are no longer present on disk."""
import sys, zipfile, re, glob, collections
sys.stdout.reconfigure(encoding='utf-8')

DIR = r'C:\Users\syson\Documents\Code\Unity-PassthroughCameraApiSamples\Dissertation'

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

OLD = ['2-P2', '3-P3', '4-P4', '9-P9', '10-P10', '13-the P13 participant',
       '15-16-p15', '17-the P17 participant', '20-P20', '25-P25', '26-P26', '27-Josh',
       '30-Pri', '33-P33', '41', '43']
NEW = ['51', '90', '1', '6']

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
        path = glob.glob(DIR + rf'\post-study-questionnaire-{nm}.docx')[0]
        ans = answers(path)
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
    path = glob.glob(DIR + rf'\post-study-questionnaire-{nm}.docx')[0]
    fav = favourable(answers(path))
    a = sum(v for k, v in fav.items() if k.startswith('A'))
    an = sum(1 for k in fav if k.startswith('A'))
    print(f'  {nm}: {a}/{an}')
