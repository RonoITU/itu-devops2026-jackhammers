import argparse

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType

# This script displays the public SSH keys stored in the project.
def main():
    parser = argparse.ArgumentParser(
        description="This script displays the public SSH keys stored in the project."
    )

    parser.add_argument(
        "-k", "--apikey",
        type=str,
        required=True,
        help="Hetzner Project API key"
    )

    args = parser.parse_args()

    client = Client(
        token=args.apikey,
        application_name="get-ssh-keys.py",
        application_version="v1.0.0"
    )

    ssh_keys = client.ssh_keys.get_all()
    print("*** SSH Public Keys:")
    for key in ssh_keys:
        print(f"{key.public_key} {key.name}")

if __name__ == "__main__":
    main()
