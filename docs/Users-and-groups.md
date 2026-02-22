# Users and Groups

Essential instructions for user and group setup from the terminal on Linux. 

## Creating groups

It may be necessary to create the `admin` group beforehand. This group has full privileges through `sudo`. 

_With elevated privileges:_

```
groupadd admin
```

## Adding a present user with password and SSH key

### 1. Creating the user

We will create the user `rono` who will use the bash terminal by default, and is in the groups `users` and `admin`. 

_With elevated privileges:_

```bash
adduser --shell /bin/bash --ingroup users --ingroup admin rono
```

You will be prompted to set a password for `rono`. _If you have not yet disabled password authentication for SSH, then this should be an especially strong password._

You will also be prompted for some optional auxiliary data (name, room, phone, other) which is entirely optional. 

### 2. Adding the public SSH key

To setup SSH access for `rono`, create the `.ssh` directory for the account. Then set permissions and ownership so that `rono` can manage his own public SSH keys without `sudo` access. 

_With elevated privileges:_

```bash
mkdir /home/rono/.ssh
chmod 700 /home/rono/.ssh
chown rono:users /home/rono/.ssh
```

Create `authorized_keys` for `rono` and add the initial public key. Afterwards set permissions and ownership for the file for `rono` to have access. 

_With elevated privileges:_

```bash
nano /home/rono/.ssh/authorized_keys # Paste and save pubkey
chmod 600 /home/rono/.ssh/authorized_keys
chown rono:users /home/rono/.ssh/authorized_keys
```

> For keys to new SSH setups, I like to use an `ed25519` keypair. If legacy compatibility was an issue, I would use an `rsa` keypair. 

## Adding a user with only an SSH key initially

In this case, `rono` is not present to set the password himself. I want to create the user, add a public SSH key, and require `rono` to set a password on the first login attempt. 

_With elevated privileges:_

```bash
adduser --disabled-password --shell /bin/bash --ingroup users --ingroup admin rono
```

> Without a password set, it is impossible to use password login for this user in any case, whether locally or remotely. 

I then follow the same steps as before to setup SSH: [[Users and Groups#2. Adding the public SSH key|Adding the public SSH key.]]

To force `rono` to set a password on the first login attempt, run this command to expire the "password":

```
passwd -e rono
```

> If SSH password authentication is allowed on the system, the password that `rono` sets will allow SSH login!

