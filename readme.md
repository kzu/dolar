# dolar container app

A minimalistic app that exposes the USD > ARS echange rates (yeah, we have more than one ¯\_ (ツ)_/¯).

## How to clone and deploy

Instructions use the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/) exclusively,
since that gives you the most control over the entire deployment.

1. **Resource Group**: make sure you are using the right account/subscription from the CLI.
   a. Show currently selected account: `az account show`
   b. If you want to change the subscription, use: `az account set --subscription <subscription-id>`. 
      To lookup the subscription id, use `az account list --output table`.
   c. Create resource group: `az group create --name dolar --location <location>`.
      To list available locations, use `az account list-locations --output table`.
   d. Ensure CLI extensions install automatically: `az config set extension.use_dynamic_install=yes_without_prompt`.

2. **Log Analytics**: setup a new log analytics workspace for logs from the app.
   a. Create it: `az monitor log-analytics workspace create -g dolar -n dolar`. 
      Copy the `customerId` value from the returned payload.
   b. Get shared key: `az monitor log-analytics workspace get-shared-keys -g dolar -n dolar`

3. **Container App Environment**: create and configure the app environment with the logs workspace created above.
   a. List available locations for container app environments: `az provider show -n Microsoft.App --query "resourceTypes[?resourceType=='managedEnvironments'].locations"`
   b. Create with: `az containerapp env create -g dolar -n dolar --logs-workspace-id [customerId] --logs-workspace-key [sharedKey] --location <location>`

4. **Container Registry**: deployments to an app come from images in a container registry, which 
   is populated automatically from CI.
   a. Create with: `az acr create -g dolar -n dolar --sku Basic --location <location>` 
      It's probably a good idea to use the same location as the app environment.
      Note the `loginServer` value in the returned payload.
   b. Login to it with: `az acr login -n dolar`
   c. Enable admin mode (so we can get the passwords) with: `az acr update -n dolar --admin-enabled true`
   d. Retrieve the username/password for the registry with: `az acr credential show -n dolar`

5. **Container App**: finally!
   a. Create with: `az containerapp create -g dolar -n dolar --environment dolar`
   b. Enable HTTP ingress with: `az containerapp ingress enable -g dolar -n dolar --type external --allow-insecure --target-port 80`
      Note: we don't really need HTTPS since Azure will automatically provide a proper HTTPS endpoint. 
   c. Set the container registry to use: `az containerapp registry set -g dolar -n dolar --server <loginServer> --username dolar --password <password>`
      Note the `--server` argument is the `loginServer` from the previous section.

6. **GitHub**: on to the setup on the repo side.
   a. Clone the repo
   b. Create an Actions repository secret named `AZURE_CONTAINER_PWD` with the container registry password from step 4.b.
   c. Create credentials to update the resource group from CI:
      `az ad sp create-for-rbac --name dolar --role contributor --scopes "/subscriptions/<subscription>/resourceGroups/dolar" --sdk-auth`
      Copy the entire response payload.
   d. Create an Actions repository secret named `AZURE_CREDENTIALS` with the copied value.
   e. If you changed resource names, update the [build.yml](.github/workflows/build.yml) file accordingly.

Now you can run the `build` workflow and see it live!