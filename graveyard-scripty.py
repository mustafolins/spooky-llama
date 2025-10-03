import requests
import time

API_BASE = "https://localhost:7233/api/spookyllama"


def generate_spooky_response(prompt):
    payload = {"prompt": prompt}
    response = requests.post(API_BASE, json=payload, verify=False)
    response.raise_for_status()
    print(f"Spooky response generated for prompt: '{prompt}'")


def get_latest_response():
    response = requests.get(f"{API_BASE}/response", verify=False)
    response.raise_for_status()
    return response.text


if __name__ == "__main__":
    import urllib3

    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

    prompts = [
        "Tell me a spooky story or continue it about Clair A. Voyant 'she didn't see this coming'",
        "Tell me a spooky story or continue it about Barry D. Hatchet",
        "Tell me a spooky story or continue it about Nora Manes",
        "Tell me a spooky story or continue it about Barry A. Live 'No fun while I was around'",
    ]

    while True:
        for prompt in prompts:
            try:
                generate_spooky_response(prompt)
                spooky_response = get_latest_response()
                print("Latest Spooky Response:")
                print(spooky_response)
                print("-" * 40)
            except:
                print('Error encountered')
        time.sleep(1)
