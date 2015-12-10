using UnityEngine;
using System.Collections;

[RequireComponent (typeof (NavMeshAgent))]
public class Enemy : LivingEntity 
{
	public enum State { Idle, Chasing, Attacking };
	State currentState;

	NavMeshAgent pathFinder;
	Transform target;
	LivingEntity targetEntity;
	Material skinMaterial;

	Color origionalColor;

	float attackDistanceThreshold = 0.5f;
	float timeBetweenAttacks = 1f;
	float damage = 1f;

	float nextAttackTime;
	float myCollisionRadius;
	float targetCollisionRadius;

	bool hasTarget;

	protected override void Start () 
	{
		base.Start ();
		pathFinder = GetComponent <NavMeshAgent> ();
		skinMaterial = GetComponent<Renderer> ().material;
		origionalColor = skinMaterial.color;

		if (GameObject.FindGameObjectWithTag ("Player") != null)
		{
			hasTarget = true;
			currentState = State.Chasing;
			target = GameObject.FindGameObjectWithTag ("Player").transform;
			targetEntity = target.GetComponent<LivingEntity> ();
			targetEntity.OnDeath += OnTargetDeath;

			myCollisionRadius = GetComponent<CapsuleCollider> ().radius;
			targetCollisionRadius = GetComponent<CapsuleCollider> ().radius;

			StartCoroutine (UpdatePath ());
		}
	}

	void OnTargetDeath ()
	{
		hasTarget = false;
		currentState = State.Idle;
	}

	void Update ()
	{
		if (hasTarget)
		{
			if (Time.time > nextAttackTime) 
			{
				float sqrDistanceToTarget = (target.position - transform.position).sqrMagnitude;	
				if (sqrDistanceToTarget < Mathf.Pow (attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2))
				{
					nextAttackTime = Time.time + timeBetweenAttacks;
					StartCoroutine (Attack ());
				}
			}
		}
	}

	IEnumerator Attack ()
	{
		currentState = State.Attacking;
		pathFinder.enabled = false;

		Vector3 origionalPosition = transform.position;
		Vector3 dirToTarget = (target.position - transform.position).normalized;
		Vector3 attackPosition = target.position - dirToTarget * (myCollisionRadius);

		float attackSpeed = 3f;
		float percent = 0f;

		skinMaterial.color = Color.red;

		bool hasAppliedDamage = false;

		while (percent <= 1f)
		{
			if (percent >= .5f && !hasAppliedDamage)
			{
				hasAppliedDamage = true;
				targetEntity.TakeDamage (damage);
			}

			percent += Time.deltaTime * attackSpeed;

			//y = 4(-x^2+x) parabola
			float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
			transform.position = Vector3.Lerp (origionalPosition, attackPosition, interpolation);

			yield return null;
		}

		skinMaterial.color = origionalColor;
		currentState = State.Chasing;
		pathFinder.enabled = true;
	}

	IEnumerator UpdatePath ()
	{
		float refreshRate = 0.25f;

		while (hasTarget) 
		{
			if (currentState == State.Chasing)
			{
				Vector3 dirToTarget = (target.position - transform.position).normalized;
				Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold/2f);

				if (!dead)
				{
					pathFinder.SetDestination (targetPosition);
				}
			}
			yield return new WaitForSeconds (refreshRate);
		}
	}
}
