import argparse
import sys

from hcloud import Client
from hcloud.images import Image
from hcloud.server_types import ServerType

# This script displays the status of all project servers.
def main():
    parser = argparse.ArgumentParser(
        description="This script displays the status of all project servers."
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
        application_name="display-servers.py",
        application_version="v1.0.0"
    )

    # List your servers
    servers = client.servers.get_all()
    print("*** Active servers:")
    for server in servers:
        print(f"{server.id=} {server.name=} {server.status=}")

if __name__ == "__main__":
    main()
