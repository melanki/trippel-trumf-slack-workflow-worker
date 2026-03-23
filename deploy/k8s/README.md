# k3s deployment

This folder contains a production-oriented base deployment for `melanki.trippeltrumf.service` on k3s.

## Manifests

- `namespace.yaml`: namespace for the worker (`trippel-trumf`)
- `serviceaccount.yaml`: dedicated service account with API token automount disabled
- `configmap.yaml`: non-secret runtime environment values
- `deployment.yaml`: single-replica worker deployment with resource limits and rolling updates
- `kustomization.yaml`: convenience entrypoint for `kubectl apply -k`

## Required secret

`TrippelTrumfService:SlackWorkflowWebhookUrl` must come from a Kubernetes secret.

Create/update it before applying the deployment:

```bash
kubectl -n trippel-trumf create secret generic trippel-trumf-worker-secrets \
  --from-literal=TrippelTrumfService__SlackWorkflowWebhookUrl='https://hooks.slack.com/triggers/...' \
  --dry-run=client -o yaml | kubectl apply -f -
```

## Apply to cluster

```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/serviceaccount.yaml
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/deployment.yaml
```

Or with kustomize:

```bash
kubectl apply -k deploy/k8s
```

To deploy a specific immutable image tag (recommended):

```bash
kubectl -n trippel-trumf set image deployment/trippel-trumf-worker \
  worker=ghcr.io/melanki/trippel-trumf-slack-workflow-worker:<commit-sha>
kubectl -n trippel-trumf rollout status deployment/trippel-trumf-worker --timeout=300s
```

## Validate

```bash
kubectl -n trippel-trumf get pods
kubectl -n trippel-trumf logs deploy/trippel-trumf-worker --tail=100
kubectl -n trippel-trumf describe deployment trippel-trumf-worker
```

## Rollback

Rollback to previous ReplicaSet:

```bash
kubectl -n trippel-trumf rollout undo deployment/trippel-trumf-worker
kubectl -n trippel-trumf rollout status deployment/trippel-trumf-worker --timeout=300s
```

Rollback to a known previous image tag:

```bash
kubectl -n trippel-trumf set image deployment/trippel-trumf-worker \
  worker=ghcr.io/melanki/trippel-trumf-slack-workflow-worker:<previous-commit-sha>
kubectl -n trippel-trumf rollout status deployment/trippel-trumf-worker --timeout=300s
```
