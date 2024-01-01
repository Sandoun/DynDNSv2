# DynDNSv2

This is a docker wrapper arround the DynDNS v2 API that automatically updates the DNS server whenever the public IP of the system changes.

# Usage

## Docker Compose

This is an example using the strato.de DynDNS api

```yaml
version: "3"
services:
    dyndnsv2:
        image: ghcr.io/sandoun/dyndnsv2:latest
        container_name: dyndnsv2
        environment:
        - UPDATE_URL=https://dyndns.strato.com/nic/update
        - USERNAME=*YOUR USERNAME*
        - PASSWORD=*YOUR PASSWORD*
        - UPDATE_DOMAIN_1=domain.com
        - UPDATE_DOMAIN_2=subdomain1.domain.com
        - UPDATE_DOMAIN_3=subdomain2.domain.com
        #add more by incrementing by 1
```

## Environment Variables

|Name|Description|Example|
|----|-----|-------|
|UPDATE_URL|The update URL of your provider|https://dyndns.strato.com/nic/update|
|USERNAME|Your login username|Username|
|PASSWORD|Your login password|Password123|
|UPDATE_DOMAIN_[N]|A domain to update, it starts with N=1 and increments by 1 for each additional one|domain.com