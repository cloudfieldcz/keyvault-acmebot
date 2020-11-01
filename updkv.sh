#!/bin/bash

KV=cf-dev2-acme-7uiy

az keyvault certificate list --vault-name $KV --query '[*].{id:id,tags:tags}' | jq -c '.[]' | while read i
do
	MYID=$(echo $i | jq '.id' -r)
	MYTAGS=$(echo $i | jq '.tags' | jq 'to_entries | map(.key + "=" + (.value | tostring) +"") | .[]' -r | tr '\n' ' ')
	if [[ $MYTAGS == *"#NO-FD-"* ]]; then
		echo "Updating $MYID .."
		MYTAGS=${MYTAGS/\#NO-FD-/\#CERTIFICATE-}
		az keyvault certificate set-attributes --id ${MYID} --tags ${MYTAGS} 
	fi
	echo $MYTAGS
done
