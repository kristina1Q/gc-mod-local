import sys, os, base64, zlib, json, hashlib
from Crypto.Cipher import DES3
import UnityPy

sys.stdout.reconfigure(encoding='utf-8')

MASTER_BUNDLE_PATH = r"c:\Users\34632\girlscreation_r\GC_Data\Caches\master.dmm\4a73e44b54688e47feb4ea2997dd6904\__data"
MASTER_JSON_PATH = r"c:\修复\local\translations\zh-Hans\master.json"

KEY_STR = 'DMM.OLG.Unity.Engine.MasterLoader'
KEY_MD5 = hashlib.md5(KEY_STR.encode('utf-8')).digest()
DES = DES3.new(KEY_MD5, DES3.MODE_ECB)

def decrypt_master(b64_cipher):
    cipher_bytes = base64.b64decode(b64_cipher)
    decrypted = DES.decrypt(cipher_bytes)
    pad = decrypted[-1]
    if pad <= 8:
        decrypted = decrypted[:-pad]
    deflate_bytes = base64.b64decode(decrypted)
    json_bytes = zlib.decompress(deflate_bytes, -zlib.MAX_WBITS)
    return json.loads(json_bytes.decode('utf-8'))

print(f"Loading master bundle from {MASTER_BUNDLE_PATH}...")
env = UnityPy.load(MASTER_BUNDLE_PATH)

tables = {}
for obj in env.objects:
    if obj.type.name == 'TextAsset':
        data = obj.read()
        if data.m_Name in ['mActiveSkills', 'mPassiveSkills', 'mArtPieces', 'mEquipments', 'mEquipmentSkills']:
            try:
                tables[data.m_Name] = decrypt_master(data.m_Script)
                print(f"Decrypted {data.m_Name}: {len(tables[data.m_Name])} records")
            except Exception as e:
                print(f"Failed to decrypt {data.m_Name}: {e}")

# Save raw decrypted skills to gc-tool for reference
os.makedirs(r"c:\修复\gc-tool\dumped_master", exist_ok=True)
for name, data in tables.items():
    with open(rf"c:\修复\gc-tool\dumped_master\{name}.json", "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

# Now check against master.json
with open(MASTER_JSON_PATH, "r", encoding="utf-8") as f:
    master_trans = json.load(f)

# Analyze mActiveSkills
act_skills = tables.get('mActiveSkills', [])
existing_act_names = master_trans.get('mActiveSkills', {}).get('ml_name1[]', {})
existing_act_descs = master_trans.get('mActiveSkills', {}).get('ml_description[]', {})

missing_act_names = {}
missing_act_descs = {}

for s in act_skills:
    # ml_name1
    name_list = s.get('ml_name1', [])
    if name_list and name_list[0]:
        n = name_list[0]
        if n not in existing_act_names:
            missing_act_names[n] = s['id']
            
    # ml_description
    desc_list = s.get('ml_description', [])
    if desc_list and desc_list[0]:
        d = desc_list[0]
        if d not in existing_act_descs:
            missing_act_descs[d] = s['id']

print(f"\n--- Active Skills Analysis ---")
print(f"Total raw active skills: {len(act_skills)}")
print(f"Existing translated names: {len(existing_act_names)}, Missing: {len(missing_act_names)}")
print(f"Existing translated descriptions: {len(existing_act_descs)}, Missing: {len(missing_act_descs)}")

# Analyze mPassiveSkills
pas_skills = tables.get('mPassiveSkills', [])
existing_pas_names = master_trans.get('mPassiveSkills', {}).get('ml_name[]', {})
existing_pas_descs = master_trans.get('mPassiveSkills', {}).get('ml_description[]', {})

missing_pas_names = {}
missing_pas_descs = {}

for s in pas_skills:
    name_list = s.get('ml_name', [])
    if name_list and name_list[0]:
        n = name_list[0]
        if n not in existing_pas_names:
            missing_pas_names[n] = s['id']
            
    desc_list = s.get('ml_description', [])
    if desc_list and desc_list[0]:
        d = desc_list[0]
        if d not in existing_pas_descs:
            missing_pas_descs[d] = s['id']

print(f"\n--- Passive Skills Analysis ---")
print(f"Total raw passive skills: {len(pas_skills)}")
print(f"Existing translated names: {len(existing_pas_names)}, Missing: {len(missing_pas_names)}")
print(f"Existing translated descriptions: {len(existing_pas_descs)}, Missing: {len(missing_pas_descs)}")

# Output summary to JSON files for review
with open(r"c:\修复\gc-tool\missing_active_skills.json", "w", encoding="utf-8") as f:
    json.dump({
        "missing_names": list(missing_act_names.keys()),
        "missing_descriptions": list(missing_act_descs.keys())
    }, f, ensure_ascii=False, indent=2)

with open(r"c:\修复\gc-tool\missing_passive_skills.json", "w", encoding="utf-8") as f:
    json.dump({
        "missing_names": list(missing_pas_names.keys()),
        "missing_descriptions": list(missing_pas_descs.keys())
    }, f, ensure_ascii=False, indent=2)
