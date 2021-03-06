﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShittyWizard.Controller.Game;

public enum EntityType {
    Player,
    Enemy,
    None
}

public class Entity : MonoBehaviour {

    [Header("Basic Settings")]
    public EntityType type;
	public AudioClip[] footsteps;
	public float footstepsVolume;
	public bool knockedBackOnHit;
	public GameObject corpsePrefab;

    [Header("State Settings")]
    public bool inControl = true;
    public float health;
    public float maxHealth = 0;
	public bool invulnerable = false;

    [Header("Bounce Settings")]
    public float bounceSpeedMultiplier = 3;
    public float bounceHeight = 0.3f;

	[Header("Particle Effects")]
	public GameObject onDeathParticle;

    public GameObject sprite;

    private float blinkRate = 0.2f;
    private bool visible = true;
    private bool flashing = false;
	private bool invisibleFlashing = false;

    private Rigidbody rb;
    private float bounceCycle = 0;
    private Vector3 spriteStartPos;

	protected GUIController _ui;

    private IceTileManager itm;
    private bool collidingWithWall = false;

	private bool previousFootstepPos = false;

	private Vector3 spriteScale;
	private GameObject spriteGO;

	protected bool alive = true;

    public void Start() {
        rb = GetComponent<Rigidbody>();
        spriteStartPos = sprite.transform.localPosition;
        maxHealth = health;
		_ui = GameObject.Find ("GUIController").GetComponent<GUIController> ();
        itm = GetComponent<IceTileManager>();
        if (transform.Find("Sprite") != null) {
            spriteGO = transform.Find("Sprite").gameObject;
            spriteScale = spriteGO.transform.localScale;
        }
        OnStart();
    }
    protected virtual void OnStart() { }

    public void Update() {

        OnUpdate();

        float bounceSpeed = rb.velocity.magnitude * bounceSpeedMultiplier;
        if (bounceSpeed == 0) {
            bounceCycle = 0;
        }
        bounceCycle += bounceSpeed * Time.deltaTime;
		float sinVal = Mathf.Sin (bounceCycle);
		sprite.transform.localPosition = spriteStartPos + Vector3.up * bounceHeight * Mathf.Abs(sinVal);

		if (sprite.transform.localPosition.y < spriteStartPos.y + 0.1f 
			&& footsteps.Length > 0
			&& bounceSpeed != 0
			&& previousFootstepPos != sinVal > 0.0f) {
			AudioSource.PlayClipAtPoint (footsteps[Random.Range(0, footsteps.Length)], sprite.transform.position, footstepsVolume * Mathf.Pow(rb.velocity.magnitude, 0.5f));
		}

		previousFootstepPos = sinVal > 0.0f;


		if (spriteGO != null && inControl && Mathf.Abs(rb.velocity.x) > 0) {
			float direction = Mathf.Sign(rb.velocity.x);
			spriteGO.transform.localScale = new Vector3 (direction * spriteScale.x, spriteGO.transform.localScale.y, spriteGO.transform.localScale.z);
		}
    }

    protected virtual void OnUpdate() { }

    public void Move(Vector3 speed) {
        if (!inControl) return;
        MoveOverride(speed);
    }

    private void MoveOverride(Vector3 speed) {
        rb.velocity = speed;
    }

	public void Damage(float _amount) {

        if (invulnerable) return;

        health -= _amount;

        if (health <= 0) {
            OnDeath();
        }

		OnDamage();

    }

	public void Heal(float _amount) {

		health += _amount;

		if (health > maxHealth) {
			health = maxHealth;
		}

	}

    protected virtual void OnDeath() { 
		alive = false;
		if (onDeathParticle != null) {
			GameObject particle = Instantiate (onDeathParticle);
			particle.transform.position = transform.position + new Vector3(0.0f, 0.15f, 0.0f);

			if (corpsePrefab != null) {
				GameObject corpse = Instantiate (corpsePrefab, transform.parent);
				corpse.transform.position = new Vector3 (transform.position.x, 0.01f, transform.position.z);
			}

			Destroy (particle, 1.5f);
		}

	}
	protected virtual void OnDamage() { }

    protected void MakeInvulnerable(float _length) {
        StartCoroutine(InvulnerableCR(_length));
    }

    private IEnumerator InvulnerableCR(float _length) {

        invulnerable = true;
        SetVisible(false);

        float blinkTimer = 0;
        while (_length > 0 || !visible) {

            _length -= Time.deltaTime;

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkRate) {
                blinkTimer -= blinkRate;
                ToggleVisibile();
                if (visible && _length < blinkRate) {
                    _length = 0;
                }
            }

            yield return null;

        }

        SetVisible(true);
        invulnerable = false;

    }

	protected void MakeInvisibleFlash(float _length) {
		if (!invisibleFlashing) {
			StartCoroutine(InvisibleFlashCR(_length));
		}
	}

	private IEnumerator InvisibleFlashCR(float _length) {

		invisibleFlashing = true;
		SetVisible(false);

		float blinkTimer = 0;
		while (_length > 0 || !visible) {

			_length -= Time.deltaTime;

			blinkTimer += Time.deltaTime;
			if (blinkTimer >= blinkRate) {
				blinkTimer -= blinkRate;
				ToggleVisibile();
				if (visible && _length < blinkRate) {
					_length = 0;
				}
			}

			yield return null;

		}

		SetVisible(true);
		invisibleFlashing = false;

	}

    private void ToggleVisibile() {
        SetVisible(!visible);
    }

    private void SetVisible(bool _visible) {
        visible = _visible;
        MeshRenderer meshRenderer = sprite.GetComponent<MeshRenderer>();
		if (meshRenderer != null) {
			meshRenderer.enabled = _visible;
		} else {
			// sorry about your elegant solution jeff...
			// things got messy
			// -james
			SpriteRenderer spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
			spriteRenderer.enabled = _visible;
		}
    }

    protected void MakeFlash(float _length) {
        if (!flashing) {
            StartCoroutine(FlashCR(_length));
        }
    }

    private IEnumerator FlashCR(float _length) {

        flashing = true;

        Renderer r = GetComponentInChildren<Renderer>();
        r.material.SetColor("_EmissionColor", Color.white);

        while (_length > 0) {
            _length -= Time.deltaTime;
            yield return null;
        }

        r.material.SetColor("_EmissionColor", Color.black);

        flashing = false;

    }

    public void Knockback(Vector3 _dir, float _distance) {
        StartCoroutine(KnockbackCR(_dir, _distance));
    }

    private IEnumerator KnockbackCR(Vector3 _dir, float _distance) {

        inControl = false;
        Vector3 startPos = this.transform.position;
        float baseSpeed = 5;

        collidingWithWall = false;

		EnemyController ec = GetComponent<EnemyController> ();
		if (ec != null) {
			ec.enabled = false;
		}

        float breakTime = 0.5f;
        float breakTimer = 0;
        float distanceTraveled = Vector3.Distance(startPos, this.transform.position);
        while (_distance - distanceTraveled > 0.5f && !inControl && breakTimer < breakTime) {

            if (collidingWithWall) {
                _distance /= 2;
                collidingWithWall = false;
            }

			if (itm != null && itm.onIce) {
                breakTime = 1f;
                _distance += Vector3.Dot(rb.velocity, _dir) * Time.deltaTime;
            } else {
                breakTime = 0.5f;
            }

            breakTimer += Time.deltaTime;
            distanceTraveled = Vector3.Distance(startPos, this.transform.position);
            this.MoveOverride(_dir * (_distance - distanceTraveled) * baseSpeed);

            yield return null;

        }

		if (ec != null) {
			ec.enabled = true;
		}

        inControl = true;

    }

    private void OnCollisionStay(Collision collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall")) {
            collidingWithWall = true;
        }
    }

}
